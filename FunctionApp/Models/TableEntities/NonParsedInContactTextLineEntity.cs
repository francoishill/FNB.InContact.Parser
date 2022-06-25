using Microsoft.Azure.Cosmos.Table;

namespace FNB.InContact.Parser.FunctionApp.Models.TableEntities;

public class NonParsedInContactTextLineEntity : TableEntity
{
    public string TextLine { get; set; }
}