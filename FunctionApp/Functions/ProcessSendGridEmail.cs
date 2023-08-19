using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Models.BusMessages;
using FNB.InContact.Parser.FunctionApp.Models.ServiceResults;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace FNB.InContact.Parser.FunctionApp.Functions;

public static class ProcessSendGridEmail
{
    [FunctionName("ProcessSendGridEmail")]
    public static async Task RunAsync(
        [ServiceBusTrigger("%ReceivedSendGridEmailsQueueName%")] string receivedSendGridEmailJson,
        ILogger log,
        [Table("ParsedInContactTextLines")] IAsyncCollector<ParsedInContactTextLineEntity> parsedEntitiesCollector,
        [Table("NonParsedInContactTextLines")] IAsyncCollector<NonParsedInContactTextLineEntity> nonParsedEntitiesCollector,
        [Table("RawTextOfParsedLines")] IAsyncCollector<RawTextOfParsedLineEntity> rawTextOfParsedLineEntitiesCollector,
        [SendGrid(ApiKey = "SendGridApiKey", From = "%FromEmailAddress%", To = "%ToEmailAddress%")] IAsyncCollector<SendGridMessage> emailMessageCollector,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# ServiceBus queue trigger function processed message: {Message}", receivedSendGridEmailJson);

        var receivedSendGridEmail = JsonConvert.DeserializeObject<ReceivedSendGridEmailBusMessage>(receivedSendGridEmailJson);

        var extractor = new InContactHttpRequestExtractor(log);

        var bodyStream = new MemoryStream((Encoding.UTF8).GetBytes(receivedSendGridEmail.RequestBody));

        var extractedLines = await extractor.ExtractInContactLines(
            bodyStream,
            receivedSendGridEmail.RequestHasFormContentType,
            cancellationToken);

        var parser = new InContactTextParser(log);

        var parsedEntities = new List<ParsedInContactLine>();
        var nonParsedEntities = new List<string>();

        var accountsWithLowAvailableFunds = new List<(string AccountNumber, string PartialCardNumber, double Available)>();
        foreach (var textLine in extractedLines)
        {
            try
            {
                var parsedEntity = parser.ParseInContactLines(textLine);

                var partitionKey = ParsedInContactTextLineEntity.IN_CONTACT_PRIMARY_KEY;
                var rowKey = Guid.NewGuid().ToString();

                await parsedEntitiesCollector.AddAsync(new ParsedInContactTextLineEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    Direction = parsedEntity.Direction.ToString(),
                    Amount = parsedEntity.Amount,
                    Action = parsedEntity.Action,
                    AccountType = parsedEntity.AccountType,
                    AccountNumber = parsedEntity.AccountNumber,
                    PartialCardNumber = parsedEntity.PartialCardNumber,
                    Method = parsedEntity.Method,
                    Available = parsedEntity.Available,
                    Reference = parsedEntity.Reference,
                    Date = parsedEntity.Date,
                    Time = parsedEntity.Time,
                }, cancellationToken);

                parsedEntities.Add(parsedEntity);

                await rawTextOfParsedLineEntitiesCollector.AddAsync(new RawTextOfParsedLineEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    TextLine = textLine,
                }, cancellationToken);

                if (parsedEntity.Available is < 1000)
                {
                    accountsWithLowAvailableFunds.Add((parsedEntity.AccountNumber, parsedEntity.PartialCardNumber, parsedEntity.Available.Value));
                }
            }
            catch (UnableToParseInContactTextException)
            {
                if (IsTextLineIgnorable(textLine))
                {
                    log.LogWarning(
                        "The following text line could not be parsed but is marked as ignorable, so it will not be added to nonParsedEntities: '{TextLine}'",
                        textLine);
                    continue;
                }

                await nonParsedEntitiesCollector.AddAsync(new NonParsedInContactTextLineEntity
                {
                    PartitionKey = NonParsedInContactTextLineEntity.IN_CONTACT_PRIMARY_KEY,
                    RowKey = Guid.NewGuid().ToString(),
                    TextLine = textLine,
                }, cancellationToken);

                nonParsedEntities.Add(textLine);
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Unable to parse text line, error: {Error}, stack: {Stack}", exception.Message, exception.StackTrace);
            }
        }

        log.LogInformation("Successfully parsed {Count} text lines", parsedEntities.Count);

        if (nonParsedEntities.Count > 0)
        {
            log.LogWarning("Failed to parse {Count} text lines", nonParsedEntities.Count);
        }

        if (accountsWithLowAvailableFunds.Any())
        {
            var grouped = accountsWithLowAvailableFunds.GroupBy(x => new { x.AccountNumber, x.PartialCardNumber });

            foreach (var group in grouped)
            {
                var lowestAmount = group.Min(x => x.Available);

                var emailSubject = $"Low available funds in FNB account {group.Key.AccountNumber} (card {group.Key.PartialCardNumber})";
                var emailHtmlBody = $"Only R {lowestAmount} available in {group.Key.AccountNumber} (card {group.Key.PartialCardNumber})";

                var message = new SendGridMessage();

                message.SetSubject(emailSubject);
                message.AddContent("text/html", emailHtmlBody);

                await emailMessageCollector.AddAsync(message, cancellationToken);
            }

            await emailMessageCollector.FlushAsync(cancellationToken);
        }
    }

    private static bool IsTextLineIgnorable(string textLine)
    {
        var cssPattern1 = new Regex(@"\.[a-zA-Z]+\s*\{\s*color", RegexOptions.Compiled);

        if (cssPattern1.IsMatch(textLine))
        {
            return true;
        }

        return false;
    }
}