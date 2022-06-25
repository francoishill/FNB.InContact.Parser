using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace FNB.InContact.Parser.FunctionApp.Services;

public class InContactTextParser
{
    private readonly ILogger _logger;

    public InContactTextParser(ILogger logger)
    {
        _logger = logger;
    }

    public ParsedInContactLine ParseInContactLines(string text)
    {
        var patterns = new[]
        {
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>paid from) (?<AccountType>.+) a\/c\.\.(?<AccountNumber>.+) @ (?<Method>.+)\. Avail R(?<Available>[\d\.]+)\. Ref\.(?<Reference>[^\.]+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>paid to) (?<AccountType>.+) a\/c\.\.(?<AccountNumber>.+) @ (?<Method>.+)\. Ref\.(?<Reference>[^\.]+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>reserved for purchase) @ (?<Reference>[^\.]+) from (?<AccountType>[^\.]+) a\/c\.\.(?<AccountNumber>.+) using (?<Method>.+)\.\.(?<PartialCardNumber>.+)\. Avail R(?<Available>[\d\.]+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) (?<Action>REVERSAL of) R(?<Amount>[\d\.]+) for (?<Reference>[^\.]+) from (?<AccountType>[^\.]+) a\/c\.\.(?<AccountNumber>.+) using (?<Method>.+)\.\.(?<PartialCardNumber>.+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
            new Regex(@"FNB\s?:-?\) R(?<Amount>[\d\.]+) (?<Action>withdrawn from) (?<AccountType>[^\.]+) a\/c\.\.(?<AccountNumber>.+) using (?<Method>.+)\.\.(?<PartialCardNumber>.+) @ (?<Reference>[^\.]+)\. Avail R(?<Available>[\d\.]+)\. (?<Date>.+) (?<Time>.+)", RegexOptions.Compiled),
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

            return new ParsedInContactLine
            {
                Amount = amount,
                Action = match.Groups["Action"].Value,
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

    public class ParsedInContactLine
    {
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
}