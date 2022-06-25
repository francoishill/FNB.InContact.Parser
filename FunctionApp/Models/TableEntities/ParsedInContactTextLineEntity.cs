using Microsoft.Azure.Cosmos.Table;

namespace FNB.InContact.Parser.FunctionApp.Models.TableEntities;

public class ParsedInContactTextLineEntity : TableEntity
{
    public const string IN_CONTACT_PRIMARY_KEY = "InContactText";

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