using System;
using System.Linq;
using System.Text.RegularExpressions;
using FNB.InContact.Parser.FunctionApp.Models.ServiceResults;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Models.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FNB.InContact.Parser.FunctionApp.Services;

public class InContactTextParser
{
    private readonly ILogger _logger;

    public InContactTextParser(ILogger logger)
    {
        _logger = logger;
    }

    public TransactionDirection GetTransactionDirectionFromAction(string action)
    {
        var incomeActions = new[]
        {
            "paid to",
            "REVERSAL of",
        };

        var expenseActions = new[]
        {
            "paid from",
            "reserved for purchase",
            "withdrawn from",
            "t/fer from",
        };

        if (incomeActions.Any(subText => action.Contains(subText, StringComparison.OrdinalIgnoreCase)))
        {
            return TransactionDirection.Income;
        }

        if (expenseActions.Any(subText => action.Contains(subText, StringComparison.OrdinalIgnoreCase)))
        {
            return TransactionDirection.Expense;
        }

        return TransactionDirection.Unknown;
    }

    public ParsedInContactLine ParseInContactLines(string text)
    {
        var patterns = new[]
        {
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>paid to) (?<AccountType>.+) a\/c\.\.(?<AccountNumber>.+) @ (?<Method>.+)\. Ref\.(?<Reference>.+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) (?<Action>REVERSAL of) R(?<Amount>[\d\.]+) for (?<Reference>.+) from (?<AccountType>[^\.]+) a\/c\.\.(?<AccountNumber>.+) using (?<Method>.+)\.\.(?<PartialCardNumber>.+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),

            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>paid from) (?<AccountType>.+) a\/c\.\.(?<AccountNumber>.+) @ (?<Method>.+)\. Avail R(?<Available>[\d\.]+)\. Ref\.(?<Reference>.+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>reserved for purchase) @ (?<Reference>.+) from (?<AccountType>[^\.]+) a\/c\.\.(?<AccountNumber>.+) using (?<Method>.+)\.\.(?<PartialCardNumber>.+)\. Avail R(?<Available>[\d\.]+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>withdrawn from) (?<AccountType>[^\.]+) a\/c\.\.(?<AccountNumber>.+) using (?<Method>.+)\.\.(?<PartialCardNumber>.+) @ (?<Reference>.+)\. Avail R(?<Available>[\d\.]+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>t\/fer from) (?<AccountType>.+) a\/c\.\.(?<AccountNumber>.+) to (?<Reference>.+) @ (?<Method>.+)\. Avail R(?<Available>[\d\.]+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
        };

        foreach (var pattern in patterns)
        {
            var match = pattern.Match(text);

            if (!match.Success)
            {
                continue;
            }

            if (!double.TryParse(match.Groups["Amount"].Value, out var amount))
            {
                _logger.LogError("Amount value '{Amount}' cannot be parsed as a numeric value", match.Groups["Amount"].Value);
                continue;
            }

            double? available = null;
            if (!double.TryParse(match.Groups["Available"].Value, out var availableMatch))
            {
                _logger.LogWarning("Available value '{Available}' cannot be parsed as a numeric value", match.Groups["Available"].Value);
            }
            else
            {
                available = availableMatch;
            }

            var action = match.Groups["Action"].Value;

            var direction = GetTransactionDirectionFromAction(action);
            if (direction == TransactionDirection.Unknown)
            {
                _logger.LogError("Unable to determine transaction direction from Action '{Action}'", action);
                continue;
            }

            return new ParsedInContactLine
            {
                Direction = direction,
                Amount = amount,
                Action = action,
                AccountType = match.Groups["AccountType"].Value,
                AccountNumber = match.Groups["AccountNumber"].Value,
                PartialCardNumber = match.Groups["PartialCardNumber"].Value,
                Method = match.Groups["Method"].Value,
                Available = available,
                Reference = match.Groups["Reference"].Value,
                Date = match.Groups["Date"].Value,
                Time = match.Groups["Time"].Value,
            };
        }

        throw new UnableToParseInContactTextException($"Unable to parse incontact text '{text}'");
    }

    public string ReBuildRawTextLineFromParsedEntry(string action, ParsedInContactTextLineEntity entry)
    {
        var amountFormatted = $"{entry.Amount:0.00}";

        return action switch
        {
            "paid to" => $"FNB:-) R{amountFormatted} paid to {entry.AccountType} a/c..{entry.AccountNumber} @ {entry.Method}. Ref.{entry.Reference}. {entry.Date} {entry.Time}",
            "REVERSAL of" => $"FNB :-) REVERSAL of R{amountFormatted} for {entry.Reference} from {entry.AccountType} a/c..{entry.AccountNumber} using {entry.Method}..{entry.PartialCardNumber}. {entry.Date} {entry.Time}",
            "paid from" => $"FNB:-) R{amountFormatted} paid from {entry.AccountType} a/c..{entry.AccountNumber} @ {entry.Method}. Avail R{(int?)entry.Available}. Ref.{entry.Reference}. {entry.Date} {entry.Time}",
            "reserved for purchase" => $"FNB :-) R{amountFormatted} reserved for purchase @ {entry.Reference} from {entry.AccountType} a/c..{entry.AccountNumber} using {entry.Method}..{entry.PartialCardNumber}. Avail R{(int?)entry.Available}. {entry.Date} {entry.Time}",
            "withdrawn from" => $"FNB:-) R{amountFormatted} withdrawn from {entry.AccountType} a/c..{entry.AccountNumber} using {entry.Method}..{entry.PartialCardNumber} @ {entry.Reference}. Avail R{(int?)entry.Available}. {entry.Date} {entry.Time}",
            "t/fer from" => $"FNB:-) R{amountFormatted} t/fer from {entry.AccountType} a/c..{entry.AccountNumber} to {entry.Reference} @ {entry.Method}. Avail R{(int?)entry.Available}. {entry.Date} {entry.Time}",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
    }
}