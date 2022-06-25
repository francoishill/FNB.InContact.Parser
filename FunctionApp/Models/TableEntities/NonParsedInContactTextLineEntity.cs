using Microsoft.Azure.Cosmos.Table;

namespace FNB.InContact.Parser.FunctionApp.Models.TableEntities;

public class NonParsedInContactTextLineEntity : TableEntity
{
    public const string IN_CONTACT_PRIMARY_KEY = "InContactText";

    public string TextLine { get; set; }
}