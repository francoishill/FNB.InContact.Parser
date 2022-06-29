using Microsoft.Azure.Cosmos.Table;

namespace FNB.InContact.Parser.FunctionApp.Models.TableEntities;

public class RawTextOfParsedLineEntity : TableEntity
{
    public string TextLine { get; set; }
}