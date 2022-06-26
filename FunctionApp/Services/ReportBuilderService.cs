using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Helpers;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using Microsoft.Azure.Cosmos.Table;

namespace FNB.InContact.Parser.FunctionApp.Services;

public class ReportBuilderService
{
    public async Task<HtmlReportResult> GenerateHtmlReport(
        CloudTable bankReferenceMappingsTable,
        CloudTable parsedEntitiesTable,
        CloudTable nonParsedEntitiesTable,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var bankReferenceMappings = (await AzureTableHelper.GetTableRecords(bankReferenceMappingsTable, new TableQuery<BankReferenceToCategoryMappingEntity>(), cancellationToken)).ToList();

        var parsedRecordsFilter = new TableQuery<ParsedInContactTextLineEntity>().Where(
            TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(ParsedInContactTextLineEntity.PartitionKey), QueryComparisons.Equal, ParsedInContactTextLineEntity.IN_CONTACT_PRIMARY_KEY),
                TableOperators.And,
                GetFilterForDateRange(startDate, endDate)
            ));

        var parsedEntries = (await AzureTableHelper.GetTableRecords(parsedEntitiesTable, parsedRecordsFilter, cancellationToken)).ToList();

        var nonParsedRecordsFilter = new TableQuery<NonParsedInContactTextLineEntity>().Where(
            TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(NonParsedInContactTextLineEntity.PartitionKey), QueryComparisons.Equal, NonParsedInContactTextLineEntity.IN_CONTACT_PRIMARY_KEY),
                TableOperators.And,
                GetFilterForDateRange(startDate, endDate)
            ));

        var nonParsedEntries = (await AzureTableHelper.GetTableRecords(nonParsedEntitiesTable, nonParsedRecordsFilter, cancellationToken)).ToList();

        var subject = $"FNB InContact Parser: Parsed {parsedEntries.Count} ({nonParsedEntries.Count} non-parsable) entries between {startDate:dd MMM} and {endDate:dd MMM}";

        var htmlBody =
            "<h1>Summary</h1>" +
            EmailContentHelper.BuildHtmlSummaries(bankReferenceMappings, parsedEntries) +
            "<h1>Parsed entries</h1>" +
            EmailContentHelper.BuildHtmlTable(parsedEntries) +
            "<h1>Non-parsed entries</h1>" +
            EmailContentHelper.BuildHtmlTable(nonParsedEntries);

        return new HtmlReportResult
        {
            Subject = subject,
            Body = htmlBody,
        };
    }

    public class HtmlReportResult
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    private static string GetFilterForDateRange(
        DateTime startDate,
        DateTime endDate)
    {
        return TableQuery.CombineFilters(
            TableQuery.GenerateFilterConditionForDate(nameof(TableEntity.Timestamp), QueryComparisons.GreaterThanOrEqual, startDate),
            TableOperators.And,
            TableQuery.GenerateFilterConditionForDate(nameof(TableEntity.Timestamp), QueryComparisons.LessThanOrEqual, endDate)
        );
    }
}