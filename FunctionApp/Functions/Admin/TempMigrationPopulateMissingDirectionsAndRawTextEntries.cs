using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Helpers;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Models.ValueObjects;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable CollectionNeverQueried.Local

namespace FNB.InContact.Parser.FunctionApp.Functions.Admin;

public static class TempMigrationPopulateMissingDirectionsAndRawTextEntries
{
    [FunctionName("TempMigrationPopulateMissingDirectionsAndRawTextEntries")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        [Table("ParsedInContactTextLines")] CloudTable parsedEntitiesTable,
        [Table("RawTextOfParsedLines")] IAsyncCollector<RawTextOfParsedLineEntity> rawTextOfParsedLineEntitiesCollector,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var parsedRecordsFilter = new TableQuery<ParsedInContactTextLineEntity>();
        var nonMigratedEntries = (await AzureTableHelper.GetTableRecords(parsedEntitiesTable, parsedRecordsFilter, cancellationToken))
            .Where(parsedEntry => string.IsNullOrWhiteSpace(parsedEntry.Direction))
            .ToList();

        var response = new ResponseDto
        {
            TotalUnmigratedRecords = nonMigratedEntries.Count,
            TotalMigrated = 0,
            TotalFailed = 0,
            FailedEntities = new List<ParsedInContactTextLineEntity>(),
        };

        var parserService = new InContactTextParser(log);

        foreach (var parsedEntry in nonMigratedEntries)
        {
            try
            {
                var direction = parserService.GetTransactionDirectionFromAction(parsedEntry.Action);

                if (direction == TransactionDirection.Unknown)
                {
                    throw new Exception($"Unable to determine transaction direction from action '{parsedEntry.Action}', full entry is: {JsonConvert.SerializeObject(parsedEntry)}");
                }

                parsedEntry.Direction = direction.ToString();

                var rawTextLine = parserService.ReBuildRawTextLineFromParsedEntry(parsedEntry.Action, parsedEntry);

                await parsedEntitiesTable.ExecuteAsync(TableOperation.InsertOrReplace(parsedEntry), cancellationToken);

                await rawTextOfParsedLineEntitiesCollector.AddAsync(new RawTextOfParsedLineEntity
                {
                    PartitionKey = parsedEntry.PartitionKey,
                    RowKey = parsedEntry.RowKey,
                    TextLine = rawTextLine,
                }, cancellationToken);
                await rawTextOfParsedLineEntitiesCollector.FlushAsync(cancellationToken);

                response.TotalMigrated++;
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Unable to migrate entry, error: {Error}, stack trace: {Stack}", exception.Message, exception.StackTrace);
                response.TotalFailed++;
                response.FailedEntities.Add(parsedEntry);
            }
        }

        return new OkObjectResult(response);
    }

    private class ResponseDto
    {
        public int TotalUnmigratedRecords { get; set; }
        public int TotalMigrated { get; set; }
        public int TotalFailed { get; set; }
        public List<ParsedInContactTextLineEntity> FailedEntities { get; set; }
    }
}