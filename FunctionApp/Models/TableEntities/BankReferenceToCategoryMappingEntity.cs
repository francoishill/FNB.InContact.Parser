using System.Web;
using Microsoft.Azure.Cosmos.Table;

namespace FNB.InContact.Parser.FunctionApp.Models.TableEntities;

public class BankReferenceToCategoryMappingEntity : TableEntity
{
    public const string DEFAULT_MAPPING_TYPE = "RegexPattern";

    public string MappingType => PartitionKey;
    public string BankReferenceRegexPattern => HttpUtility.UrlDecode(RowKey);

    public string CategoryName { get; set; }

    public BankReferenceToCategoryMappingEntity()
    {
        // used for CloudTable queries
    }

    public BankReferenceToCategoryMappingEntity(
        string mappingType,
        string bankReferenceRegexPattern,
        string categoryName)
    {
        PartitionKey = mappingType;
        RowKey = HttpUtility.UrlEncode(bankReferenceRegexPattern);

        CategoryName = categoryName;
    }
}