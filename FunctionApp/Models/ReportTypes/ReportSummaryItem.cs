using System.Collections.Generic;

namespace FNB.InContact.Parser.FunctionApp.Models.ReportTypes;

public class ReportSummaryItem
{
    public string Summary { get; set; }
    public IEnumerable<LineItem> LineItems { get; set; }

    public ReportSummaryItem(string summary, IEnumerable<LineItem> lineItems)
    {
        Summary = summary;
        LineItems = lineItems;
    }

    public class LineItem
    {
        public string Text { get; set; }

        public LineItem(string text)
        {
            Text = text;
        }
    }
}