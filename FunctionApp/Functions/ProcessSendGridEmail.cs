using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Models.BusMessages;
using FNB.InContact.Parser.FunctionApp.Models.ServiceResults;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
            }
            catch (UnableToParseInContactTextException)
            {
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
    }
}