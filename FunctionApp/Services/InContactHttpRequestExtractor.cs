using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using StrongGrid;

namespace FNB.InContact.Parser.FunctionApp.Services;

public class InContactHttpRequestExtractor
{
    private readonly ILogger _logger;

    public InContactHttpRequestExtractor(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<string>> ExtractInContactLines(
        Stream bodyStream,
        bool hasFormContentType,
        CancellationToken cancellationToken)
    {
        using var copiedStream = new MemoryStream();
        await bodyStream.CopyToAsync(copiedStream, cancellationToken);

        if (hasFormContentType)
        {
            _logger.LogInformation("Found form body, assuming it is an inbound email from SendGrid, will now use SendGrid WebhookParser to try extract the text body");

            copiedStream.Position = 0;

            var webhookParser = new WebhookParser();
            var inboundEmail = await webhookParser.ParseInboundEmailWebhookAsync(copiedStream);

            _logger.LogInformation("Information of SendGrid parse request: SenderIp = {SenderIp}, Subject = {Subject}", inboundEmail.SenderIp, inboundEmail.Subject);

            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(inboundEmail.Html);

            var lines = SanitizeLines(document.ChildNodes
                    .Select(p => p.Text())
                    .SelectMany(p => p.Split(new[] { "<br>", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(line => line.Replace("•", "").Trim()))
                .ToList();

            if (lines.Count > 0)
            {
                _logger.LogInformation("Extracted FNB InContact lines from HTML body, lines: {Lines}", string.Join("[NEW LINE]", lines));
                return lines;
            }

            _logger.LogWarning("Expected html paragraphs to contain lines but none found, continuing to try use alternative method to extract email text");

            var emailText = inboundEmail.Text;
            if (string.IsNullOrWhiteSpace(emailText))
            {
                _logger.LogError("The parsed email text is empty");

                copiedStream.Position = 0;

                using var streamReader = new StreamReader(copiedStream);
                var fullBodyText = await streamReader.ReadToEndAsync();

                _logger.LogError("Full body text is: {Text}", fullBodyText);

                return new[] { fullBodyText };
            }

            _logger.LogInformation("Successfully extracted email text body from SendGrid email: {Body}", emailText);

            return new[] { emailText };
        }

        bodyStream.Position = 0;

        var rawBody = await new StreamReader(bodyStream).ReadToEndAsync();

        _logger.LogInformation("Body is not of type Form, assuming the raw body can be used, raw body is: {Body}", rawBody);

        return SanitizeLines(rawBody
            .Split(new[] { "\n", "<br>" }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static IEnumerable<string> SanitizeLines(IEnumerable<string> originalLines)
    {
        return originalLines
            .Select(line => line.Replace("•", "").Trim())
            .Where(line =>
                !string.IsNullOrWhiteSpace(line)
                && !line.Trim().EndsWith("Dear valued customer", StringComparison.Ordinal)
                && !line.Trim().StartsWith("Please do NOT reply to this message", StringComparison.Ordinal));
    }
}