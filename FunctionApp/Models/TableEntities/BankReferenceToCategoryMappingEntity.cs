using System;
using System.Web;
using Microsoft.Azure.Cosmos.Table;

namespace FNB.InContact.Parser.FunctionApp.Models.TableEntities;

public class BankReferenceToCategoryMappingEntity : TableEntity
{
    public TransactionDirection Direction => Enum.Parse<TransactionDirection>(PartitionKey);
    public string BankReferenceRegexPattern => HttpUtility.UrlDecode(RowKey);

    public string CategoryName { get; set; }

    public BankReferenceToCategoryMappingEntity()
    {
        // used for CloudTable queries
    }

    public BankReferenceToCategoryMappingEntity(
        TransactionDirection direction,
        string bankReferenceRegexPattern,
        string categoryName)
    {
        PartitionKey = direction.ToString();
        RowKey = HttpUtility.UrlEncode(bankReferenceRegexPattern);

        CategoryName = categoryName;
    }

    public enum TransactionDirection
    {
        Income,
        Expense,
    }
}