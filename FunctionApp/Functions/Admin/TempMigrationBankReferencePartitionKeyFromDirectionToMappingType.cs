using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Helpers;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Models.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

// ReSharper disable CollectionNeverQueried.Local

namespace FNB.InContact.Parser.FunctionApp.Functions.Admin;

public static class TempMigrationBankReferencePartitionKeyFromDirectionToMappingType
{
    [FunctionName("TempMigrationBankReferencePartitionKeyFromDirectionToMappingType")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log,
        [Table("BankReferenceToCategoryMappings")] CloudTable bankReferenceMappingsTable,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var filter = new TableQuery<BankReferenceToCategoryMappingEntity>().Where(
            TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(BankReferenceToCategoryMappingEntity.PartitionKey), QueryComparisons.Equal, TransactionDirection.Income.ToString()),
                TableOperators.Or,
                TableQuery.GenerateFilterCondition(nameof(BankReferenceToCategoryMappingEntity.PartitionKey), QueryComparisons.Equal, TransactionDirection.Expense.ToString())
            ));

        var nonMigratedRecords = (await AzureTableHelper.GetTableRecords(bankReferenceMappingsTable, filter, cancellationToken)).ToList();

        var response = new ResponseDto
        {
            TotalUnmigratedRecords = nonMigratedRecords.Count,
            TotalMigrated = 0,
            TotalFailed = 0,
            FailedEntities = new List<BankReferenceToCategoryMappingEntity>(),
        };

        foreach (var oldRecord in nonMigratedRecords)
        {
            try
            {
                var newRecord = new BankReferenceToCategoryMappingEntity(
                    BankReferenceToCategoryMappingEntity.DEFAULT_MAPPING_TYPE,
                    oldRecord.BankReferenceRegexPattern,
                    oldRecord.CategoryName);

                await bankReferenceMappingsTable.ExecuteAsync(TableOperation.Insert(newRecord), cancellationToken);
                await bankReferenceMappingsTable.ExecuteAsync(TableOperation.Delete(oldRecord), cancellationToken);

                response.TotalMigrated++;
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Unable to migrate entry, error: {Error}, stack trace: {Stack}", exception.Message, exception.StackTrace);
                response.TotalFailed++;
                response.FailedEntities.Add(oldRecord);
            }
        }

        return new OkObjectResult(response);
    }

    private class ResponseDto
    {
        public int TotalUnmigratedRecords { get; set; }
        public int TotalMigrated { get; set; }
        public int TotalFailed { get; set; }
        public List<BankReferenceToCategoryMappingEntity> FailedEntities { get; set; }
    }
}