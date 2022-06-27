using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using Microsoft.Extensions.Logging;

namespace FNB.InContact.Parser.FunctionApp.Infrastructure.Helpers;

public static class EmailContentHelper
{
    public static string BuildHtmlTable(
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

        var htmlBodyBuilder = new StringBuilder();

        htmlBodyBuilder.Append("<table style='border: 1px solid #ccc; border-collapse: collapse;'>");
        htmlBodyBuilder.Append("<thead>");

        htmlBodyBuilder.Append("<tr>");

        foreach (var cellDefinition in tableCells)
        {
            htmlBodyBuilder.Append($"<th style='border: 1px solid #ccc'>{cellDefinition.ColumnName}</th>");
        }

        htmlBodyBuilder.Append("</tr>");

        htmlBodyBuilder.Append("</thead>");

        htmlBodyBuilder.Append("<tbody>");

        var sortedBackups = entries.OrderBy(b => b.Timestamp);
        foreach (var backup in sortedBackups)
        {
            htmlBodyBuilder.Append("<tr>");

            foreach (var cellDefinition in tableCells)
            {
                htmlBodyBuilder.Append($"<td style='border: 1px solid #ccc'>{cellDefinition.ValueGetter(backup)}</td>");
            }

            htmlBodyBuilder.Append("</tr>");
        }

        htmlBodyBuilder.Append("</tbody>");
        htmlBodyBuilder.Append("</table>");

        return htmlBodyBuilder.ToString().Replace("\n", "<br>");
    }

    public static string BuildHtmlTable(
        IEnumerable<NonParsedInContactTextLineEntity> entries)
    {
        var tableCells = new[]
        {
            new HtmlTableCellDefinition<NonParsedInContactTextLineEntity>("Text Line", backup => backup.TextLine),
        };

        var htmlBodyBuilder = new StringBuilder();

        htmlBodyBuilder.Append("<table style='border: 1px solid #ccc; border-collapse: collapse;'>");
        htmlBodyBuilder.Append("<thead>");

        htmlBodyBuilder.Append("<tr>");

        foreach (var cellDefinition in tableCells)
        {
            htmlBodyBuilder.Append($"<th style='border: 1px solid #ccc'>{cellDefinition.ColumnName}</th>");
        }

        htmlBodyBuilder.Append("</tr>");

        htmlBodyBuilder.Append("</thead>");

        htmlBodyBuilder.Append("<tbody>");

        var sortedBackups = entries.OrderBy(b => b.Timestamp);
        foreach (var backup in sortedBackups)
        {
            htmlBodyBuilder.Append("<tr>");

            foreach (var cellDefinition in tableCells)
            {
                htmlBodyBuilder.Append($"<td style='border: 1px solid #ccc'>{cellDefinition.ValueGetter(backup)}</td>");
            }

            htmlBodyBuilder.Append("</tr>");
        }

        htmlBodyBuilder.Append("</tbody>");
        htmlBodyBuilder.Append("</table>");

        return htmlBodyBuilder.ToString().Replace("\n", "<br>");
    }

    public static string BuildHtmlSummaries(
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

        var summariesHtml = new StringBuilder();

        var successfullyMappedEntries = new List<ParsedInContactTextLineEntity>();
        foreach (var categoryGroup in patternsGroupedByCategory)
        {
            var categoryName = categoryGroup.Key;
            var distinctDirections = categoryGroup.Select(group => group.Mapping.Direction).Distinct().ToList();

            if (distinctDirections.Count > 0)
            {
                logger.LogWarning(
                    "Category mappings Directions are inconsistent, category '{Category}' has the following directions: {Directions}",
                    categoryName, string.Join(", ", distinctDirections.Select(d => d.ToString())));
            }

            var firstDirection = distinctDirections.First();

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

            var sign = firstDirection == BankReferenceToCategoryMappingEntity.TransactionDirection.Income ? "+" : "-";
            AppendHtmlForCategoryAndEntries(matchingEntries, categoryName, sign, summariesHtml);
        }

        var unmappedEntries = parsedEntries.Where(entry => !successfullyMappedEntries.Contains(entry)).ToList();
        AppendHtmlForCategoryAndEntries(unmappedEntries, "Unknown", "?", summariesHtml);

        return summariesHtml.ToString();
    }

    private static void AppendHtmlForCategoryAndEntries(
        List<ParsedInContactTextLineEntity> matchingEntries,
        string categoryName,
        string sign,
        StringBuilder summariesHtml)
    {
        summariesHtml.Append("<details>");

        var categoryTotalAmount = matchingEntries.Sum(entry => entry.Amount);

        var categorySummaryText = $"{categoryName} {sign}R {categoryTotalAmount}";
        summariesHtml.Append($"<summary>{categorySummaryText}</summary>");

        summariesHtml.Append("<p>");
        summariesHtml.Append("<ul>");

        foreach (var matchingEntry in matchingEntries)
        {
            summariesHtml.Append($"<li>{matchingEntry.ToSummaryString()}</li>");
        }

        summariesHtml.Append("</ul>");
        summariesHtml.Append("</p>");

        summariesHtml.Append("</details>");
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