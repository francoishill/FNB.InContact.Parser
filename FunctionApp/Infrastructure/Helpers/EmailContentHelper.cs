using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FNB.InContact.Parser.FunctionApp.Models.ReportTypes;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Models.ValueObjects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FNB.InContact.Parser.FunctionApp.Infrastructure.Helpers;

public static class EmailContentHelper
{
    public static ReportTable BuildParsedEntries(
        IEnumerable<ParsedInContactTextLineEntity> entries)
    {
        var tableCells = new[]
        {
            new HtmlTableCellDefinition<ParsedInContactTextLineEntity>("Amount", backup => backup.Amount.ToString(CultureInfo.InvariantCulture)),
            new HtmlTableCellDefinition<ParsedInContactTextLineEntity>("Action", backup => backup.Action),
            new HtmlTableCellDefinition<ParsedInContactTextLineEntity>("Reference", backup => backup.Reference),
            new HtmlTableCellDefinition<ParsedInContactTextLineEntity>("Method", backup => backup.Method),
            new HtmlTableCellDefinition<ParsedInContactTextLineEntity>("DateTime", backup => $"{backup.Date} {backup.Time}"),
            new HtmlTableCellDefinition<ParsedInContactTextLineEntity>("Available", backup => backup.Available?.ToString() ?? "null"),
            new HtmlTableCellDefinition<ParsedInContactTextLineEntity>("Account/Card", backup => $"{backup.AccountType}, {backup.AccountNumber}, {backup.PartialCardNumber}"),
        };

        var columns = new List<ReportTable.TableColumn>();
        var rows = new List<ReportTable.TableRow>();

        foreach (var cellDefinition in tableCells)
        {
            columns.Add(new ReportTable.TableColumn(cellDefinition.ColumnName));
        }

        var sortedBackups = entries.OrderBy(b => b.Timestamp);
        foreach (var backup in sortedBackups)
        {
            var rowCells = new List<ReportTable.TableCell>();

            foreach (var cellDefinition in tableCells)
            {
                rowCells.Add(new ReportTable.TableCell(cellDefinition.ValueGetter(backup)));
            }

            rows.Add(new ReportTable.TableRow(rowCells));
        }

        return new ReportTable(columns, rows);
    }

    public static ReportTable BuildNonParsedEntries(
        IEnumerable<NonParsedInContactTextLineEntity> entries)
    {
        var tableCells = new[]
        {
            new HtmlTableCellDefinition<NonParsedInContactTextLineEntity>("Text Line", backup => backup.TextLine),
        };

        var columns = new List<ReportTable.TableColumn>();
        var rows = new List<ReportTable.TableRow>();

        foreach (var cellDefinition in tableCells)
        {
            columns.Add(new ReportTable.TableColumn(cellDefinition.ColumnName));
        }

        var sortedBackups = entries.OrderBy(b => b.Timestamp);
        foreach (var backup in sortedBackups)
        {
            var rowCells = new List<ReportTable.TableCell>();

            foreach (var cellDefinition in tableCells)
            {
                rowCells.Add(new ReportTable.TableCell(cellDefinition.ValueGetter(backup)));
            }

            rows.Add(new ReportTable.TableRow(rowCells));
        }

        return new ReportTable(columns, rows);
    }

    public static IEnumerable<ReportSummaryItem> BuildSummaryItems(
        ILogger logger,
        IEnumerable<BankReferenceToCategoryMappingEntity> bankReferenceMappings,
        IReadOnlyCollection<ParsedInContactTextLineEntity> parsedEntries)
    {
        var patternWithBankReferenceMappings = bankReferenceMappings
            .OrderBy(mapping => mapping.CategoryName)
            .Select(mapping => new
            {
                Regex = new Regex(mapping.BankReferenceRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled),
                Mapping = mapping,
            })
            .ToList();

        var patternsGroupedByCategory = patternWithBankReferenceMappings.GroupBy(p => p.Mapping.CategoryName);

        var summaryItems = new List<ReportSummaryItem>();

        var successfullyMappedEntries = new List<ParsedInContactTextLineEntity>();
        foreach (var categoryGroup in patternsGroupedByCategory)
        {
            var categoryName = categoryGroup.Key;

            var matchingEntries = parsedEntries
                .Where(entry => categoryGroup.Any(group =>
                    group.Regex.IsMatch(entry.Reference)
                    || group.Regex.IsMatch(entry.Action)))
                .ToList();

            if (matchingEntries.Count == 0)
            {
                continue;
            }

            successfullyMappedEntries.AddRange(matchingEntries.Where(m => !successfullyMappedEntries.Contains(m)));

            AppendHtmlForCategoryAndEntries(logger, matchingEntries, categoryName, summaryItems);
        }

        var unmappedEntries = parsedEntries.Where(entry => !successfullyMappedEntries.Contains(entry)).ToList();

        AppendHtmlForCategoryAndEntries(logger, unmappedEntries, "Unknown", summaryItems);

        return summaryItems;
    }

    private static void AppendHtmlForCategoryAndEntries(
        ILogger logger,
        IReadOnlyCollection<ParsedInContactTextLineEntity> matchingEntries,
        string categoryName,
        ICollection<ReportSummaryItem> summaryItems)
    {
        var entriesWithInvalidDirections = matchingEntries
            .Where(entry => entry.Direction != TransactionDirection.Income.ToString() && entry.Direction != TransactionDirection.Expense.ToString())
            .ToList();
        if (entriesWithInvalidDirections.Count > 0)
        {
            var invalidDirections = entriesWithInvalidDirections.Select(e => e.Direction?.Trim() ?? "").Distinct();
            var entriesStrings = entriesWithInvalidDirections.Select(JsonConvert.SerializeObject);
            logger.LogWarning(
                "There are {Count} entries in '{Category}' category that have invalid directions, directions: {Directions}, entries: {Entries}",
                entriesWithInvalidDirections.Count, categoryName, string.Join(", ", invalidDirections), string.Join(". ", entriesStrings));
        }

        var categoryTotalAmount = matchingEntries.Sum(entry => entry.Direction == TransactionDirection.Expense.ToString() ? -entry.Amount : entry.Amount);
        var categorySummaryText = $"{categoryName} R {categoryTotalAmount}";

        var lineItems = matchingEntries.Select(matchingEntry => new ReportSummaryItem.LineItem(matchingEntry.ToSummaryString())).ToList();

        summaryItems.Add(new ReportSummaryItem(categorySummaryText, lineItems));
    }

    private class HtmlTableCellDefinition<T>
    {
        public string ColumnName { get; set; }
        public Func<T, string> ValueGetter { get; set; }

        public HtmlTableCellDefinition(
            string columnName,
            Func<T, string> valueGetter)
        {
            ColumnName = columnName;
            ValueGetter = valueGetter;
        }
    }
}