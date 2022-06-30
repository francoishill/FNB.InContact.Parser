using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Helpers;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FNB.InContact.Parser.FunctionApp.Functions.Admin;

public static class GetBankReferenceToCategoryMappings
{
    [FunctionName("GetBankReferenceToCategoryMappings")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log,
        [Table("BankReferenceToCategoryMappings")] CloudTable bankReferenceMappingsTable,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var mappingEntities = (await AzureTableHelper.GetTableRecords(bankReferenceMappingsTable, new TableQuery<BankReferenceToCategoryMappingEntity>(), cancellationToken)).ToList();

        return new OkObjectResult(mappingEntities.Select(mappingEntity => new BankReferenceToCategoryMappingDto
        {
            MappingType = mappingEntity.MappingType.ToString(),
            BankReferenceRegexPattern = mappingEntity.BankReferenceRegexPattern,
            CategoryName = mappingEntity.CategoryName,
        }));
    }

    private class BankReferenceToCategoryMappingDto
    {
        public string MappingType { get; set; }
        public string BankReferenceRegexPattern { get; set; }
        public string CategoryName { get; set; }
    }
}