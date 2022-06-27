using System.Collections.Generic;

namespace FNB.InContact.Parser.FunctionApp.Models.ReportTypes;

public class ReportTable
{
    public IEnumerable<TableColumn> Columns { get; set; }
    public IEnumerable<TableRow> Rows { get; set; }

    public ReportTable(IEnumerable<TableColumn> columns, IEnumerable<TableRow> rows)
    {
        Columns = columns;
        Rows = rows;
    }

    public class TableColumn
    {
        public string Heading { get; set; }

        public TableColumn(string heading)
        {
            Heading = heading;
        }
    }

    public class TableRow
    {
        public IEnumerable<TableCell> Cells { get; set; }

        public TableRow(IEnumerable<TableCell> cells)
        {
            Cells = cells;
        }
    }

    public class TableCell
    {
        public string Text { get; set; }

        public TableCell(string text)
        {
            Text = text;
        }
    }
}