using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FNB.InContact.Parser.FunctionApp.Functions;

public static class InContactEmailParserWebhook
{
    [FunctionName("InContactEmailParserWebhook")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        [Table("ParsedInContactTextLines")] IAsyncCollector<ParsedInContactTextLineEntity> parsedEntitiesCollector,
        [Table("NonParsedInContactTextLines")] IAsyncCollector<NonParsedInContactTextLineEntity> nonParsedEntitiesCollector)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var parser = new InContactTextParser(log);

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        var textLines = requestBody.Split("\n")
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        var parsedEntities = new List<InContactTextParser.ParsedInContactLine>();
        var nonParsedEntities = new List<string>();

        foreach (var textLine in textLines)
        {
            try
            {
                var parsedEntity = parser.ParseInContactLines(textLine);

                await parsedEntitiesCollector.AddAsync(new ParsedInContactTextLineEntity
                {
                    PartitionKey = "InContactText",
                    RowKey = Guid.NewGuid().ToString(),
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
                });

                parsedEntities.Add(parsedEntity);
            }
            catch (UnableToParseInContactTextException)
            {
                await nonParsedEntitiesCollector.AddAsync(new NonParsedInContactTextLineEntity
                {
                    PartitionKey = "InContactText",
                    RowKey = Guid.NewGuid().ToString(),
                    TextLine = textLine,
                });

                nonParsedEntities.Add(textLine);
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Unable to parse text line, error: {Error}, stack: {Stack}", exception.Message, exception.StackTrace);
            }
        }

        return new OkObjectResult(new
        {
            ParsedEntities = parsedEntities,
            NonParsedEntities = nonParsedEntities,
        });
    }

    public class ParsedInContactTextLineEntity : TableEntity
    {
        public double Amount { get; set; }
        public string Action { get; set; }
        public string AccountType { get; set; }
        public string AccountNumber { get; set; }
        public string PartialCardNumber { get; set; }
        public string Method { get; set; }
        public double? Available { get; set; }
        public string Reference { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }

    public class NonParsedInContactTextLineEntity : TableEntity
    {
        public string TextLine { get; set; }
    }
}