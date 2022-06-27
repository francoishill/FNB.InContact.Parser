using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Helpers;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Templates;
using HandlebarsDotNet;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace FNB.InContact.Parser.FunctionApp.Services;

public class ReportBuilderService
{
    public async Task<string> GenerateReportEmailSubject(
        CloudTable parsedEntitiesTable,
        CloudTable nonParsedEntitiesTable,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
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

        return subject;
    }

    public async Task<string> GenerateReportHtml(
        ILogger logger,
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

        var htmlBodyTemplate = await TemplateHelpers.GetHtmlTemplateStringAsync("ReportForDateRange.handlebars");
        var template = Handlebars.Compile(htmlBodyTemplate);

        var data = new
        {
            SummaryItems = EmailContentHelper.BuildSummaryItems(logger, bankReferenceMappings, parsedEntries),
            ParsedEntries = EmailContentHelper.BuildParsedEntries(parsedEntries),
            NonParsedEntries = EmailContentHelper.BuildNonParsedEntries(nonParsedEntries),
        };

        var htmlBody = template(data);
        return htmlBody;
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