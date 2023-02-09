using System.Collections.Generic;

namespace FNB.InContact.Parser.FunctionApp.Models.ReportTypes;

public class ReportSummaryItem
{
    public string Summary { get; set; }
    public IEnumerable<LineItem> LineItems { get; set; }
    public bool IsUnknownCategory { get; set; }

    public ReportSummaryItem(string summary, IEnumerable<LineItem> lineItems, bool isUnknownCategory)
    {
        Summary = summary;
        LineItems = lineItems;
        IsUnknownCategory = isUnknownCategory;
    }

    public class LineItem
    {
        public string ReferenceName { get; set; }
        public string Text { get; set; }

        public LineItem(string referenceName, string text)
        {
            ReferenceName = referenceName;
            Text = text;
        }
    }
}