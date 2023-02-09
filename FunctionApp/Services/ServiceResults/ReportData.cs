using System.Collections.Generic;
using FNB.InContact.Parser.FunctionApp.Models.ReportTypes;

namespace FNB.InContact.Parser.FunctionApp.Services.ServiceResults;

public class ReportData
{
    public IEnumerable<ReportSummaryItem> SummaryItems { get; init; }
    public ReportTable ParsedEntries { get; init; }
    public ReportTable NonParsedEntries { get; init; }
}