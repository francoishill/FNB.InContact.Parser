using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FNB.InContact.Parser.Tests.Services;

public class InContactEmailReBuildRawTextTests
{
    [Fact]
    public void ReBuildRawTextLineFromParsedEntry_PaidTo_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);
        var parsedEntry = new ParsedInContactTextLineEntity
        {
            Action = "paid to",
            Amount = 113355.2,
            AccountType = "My Account Type",
            AccountNumber = "112233",
            Method = "My Method",
            Reference = "My Reference Name",
            Date = "24Jun",
            Time = "00:00",
        };

        // Act
        var rawTextLine = parser.ReBuildRawTextLineFromParsedEntry(parsedEntry.Action, parsedEntry);

        // Assert
        Assert.Equal("FNB:-) R113355.20 paid to My Account Type a/c..112233 @ My Method. Ref.My Reference Name. 24Jun 00:00", rawTextLine);
    }

    [Fact]
    public void ReBuildRawTextLineFromParsedEntry_ReversalOf_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);
        var parsedEntry = new ParsedInContactTextLineEntity
        {
            Action = "REVERSAL of",
            Amount = 113355.2,
            AccountType = "My Account Type",
            AccountNumber = "112233",
            Method = "My Method",
            Reference = "Some Reference Name",
            PartialCardNumber = "7531",
            Date = "21Jun",
            Time = "11:56",
        };

        // Act
        var rawTextLine = parser.ReBuildRawTextLineFromParsedEntry(parsedEntry.Action, parsedEntry);

        // Assert
        Assert.Equal("FNB :-) REVERSAL of R113355.20 for Some Reference Name from My Account Type a/c..112233 using My Method..7531. 21Jun 11:56", rawTextLine);
    }
    
    [Fact]
    public void ReBuildRawTextLineFromParsedEntry_PaidFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);
        var parsedEntry = new ParsedInContactTextLineEntity
        {
            Action = "paid from",
            Amount = 113355.2,
            AccountType = "My Account Type",
            AccountNumber = "112233",
            Method = "My Method",
            Available = 12345,
            Reference = "My Reference Name",
            Date = "20Jun",
            Time = "03:27",
        };

        // Act
        var rawTextLine = parser.ReBuildRawTextLineFromParsedEntry(parsedEntry.Action, parsedEntry);

        // Assert
        Assert.Equal("FNB:-) R113355.20 paid from My Account Type a/c..112233 @ My Method. Avail R12345. Ref.My Reference Name. 20Jun 03:27", rawTextLine);
    }
    
    [Fact]
    public void ReBuildRawTextLineFromParsedEntry_ReservedForPurchase_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);
        var parsedEntry = new ParsedInContactTextLineEntity
        {
            Action = "reserved for purchase",
            Amount = 113355.2,
            AccountType = "My Account Type",
            AccountNumber = "112233",
            Method = "My Method",
            PartialCardNumber = "7531",
            Available = 12345,
            Reference = "My Reference Name",
            Date = "20Jun",
            Time = "03:27",
        };

        // Act
        var rawTextLine = parser.ReBuildRawTextLineFromParsedEntry(parsedEntry.Action, parsedEntry);

        // Assert
        Assert.Equal("FNB :-) R113355.20 reserved for purchase @ My Reference Name from My Account Type a/c..112233 using My Method..7531. Avail R12345. 20Jun 03:27", rawTextLine);
    }
    
    [Fact]
    public void ReBuildRawTextLineFromParsedEntry_WithdrawnFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);
        var parsedEntry = new ParsedInContactTextLineEntity
        {
            Action = "withdrawn from",
            Amount = 113355.2,
            AccountType = "My Account Type",
            AccountNumber = "112233",
            Method = "My Method",
            PartialCardNumber = "7531",
            Available = 12345,
            Reference = "My Reference Name",
            Date = "20Jun",
            Time = "03:27",
        };

        // Act
        var rawTextLine = parser.ReBuildRawTextLineFromParsedEntry(parsedEntry.Action, parsedEntry);

        // Assert
        Assert.Equal("FNB:-) R113355.20 withdrawn from My Account Type a/c..112233 using My Method..7531 @ My Reference Name. Avail R12345. 20Jun 03:27", rawTextLine);
    }
    
    [Fact]
    public void ReBuildRawTextLineFromParsedEntry_TransferFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);
        var parsedEntry = new ParsedInContactTextLineEntity
        {
            Action = "t/fer from",
            Amount = 113355.2,
            AccountType = "My Account Type",
            AccountNumber = "112233",
            Method = "My Method",
            PartialCardNumber = "7531",
            Available = 12345,
            Reference = "FNB card a/c..345654",
            Date = "20Jun",
            Time = "03:27",
        };

        // Act
        var rawTextLine = parser.ReBuildRawTextLineFromParsedEntry(parsedEntry.Action, parsedEntry);

        // Assert
        Assert.Equal("FNB:-) R113355.20 t/fer from My Account Type a/c..112233 to FNB card a/c..345654 @ My Method. Avail R12345. 20Jun 03:27", rawTextLine);
    }
}