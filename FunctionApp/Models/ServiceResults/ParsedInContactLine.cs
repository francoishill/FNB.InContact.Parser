using FNB.InContact.Parser.FunctionApp.Models.ValueObjects;

namespace FNB.InContact.Parser.FunctionApp.Models.ServiceResults;

public class ParsedInContactLine
{
    public TransactionDirection Direction { get; set; }
    public double Amount { get; set; }
    public string Action { get; set; }
    public string AccountType { get; set; }
    public string AccountNumber { get; set; }
    public string PartialCardNumber { get; set; }
    public string Method { get; set; }
    public double? Available { get; set; }
    public string Reference { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
}