using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FNB.InContact.Parser.Tests;

public class InContactEmailParserTests
{
    [Fact]
    public void ParseInContactLines_PaidFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB:-) R876.32 paid from Premier a/c..123456 @ Eft. Avail R554433. Ref.Someone I paid money to. 23Jun 02:44");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(876.32, parsedEmail.Amount, 3);
        Assert.Equal("paid from", parsedEmail.Action);
        Assert.Equal("Premier", parsedEmail.AccountType);
        Assert.Equal("123456", parsedEmail.AccountNumber);
        Assert.Empty(parsedEmail.PartialCardNumber);
        Assert.Equal("Eft", parsedEmail.Method);
        Assert.Equal(554433, parsedEmail.Available ?? 0, 3);
        Assert.Equal("Someone I paid money to", parsedEmail.Reference);
        Assert.Equal("23Jun", parsedEmail.Date);
        Assert.Equal("02:44", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_PaidTo_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB:-) R9876.21 paid to Premier a/c..123456 @ Eft. Ref.Someone that paid money to me. 24Jun 07:73");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(9876.21, parsedEmail.Amount, 3);
        Assert.Equal("paid to", parsedEmail.Action);
        Assert.Equal("Premier", parsedEmail.AccountType);
        Assert.Equal("123456", parsedEmail.AccountNumber);
        Assert.Empty(parsedEmail.PartialCardNumber);
        Assert.Equal("Eft", parsedEmail.Method);
        Assert.Null(parsedEmail.Available);
        Assert.Equal("Someone that paid money to me", parsedEmail.Reference);
        Assert.Equal("24Jun", parsedEmail.Date);
        Assert.Equal("07:73", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_ReservedFor_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB :-) R571.54 reserved for purchase @ Some Random Merchant from FNB card a/c..123000 using card..8765. Avail R3201. 23Jun 08:40");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(571.54, parsedEmail.Amount, 3);
        Assert.Equal("reserved for purchase", parsedEmail.Action);
        Assert.Equal("FNB card", parsedEmail.AccountType);
        Assert.Equal("123000", parsedEmail.AccountNumber);
        Assert.Equal("8765", parsedEmail.PartialCardNumber);
        Assert.Equal("card", parsedEmail.Method);
        Assert.Equal(3201, parsedEmail.Available ?? 0, 3);
        Assert.Equal("Some Random Merchant", parsedEmail.Reference);
        Assert.Equal("23Jun", parsedEmail.Date);
        Assert.Equal("08:40", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_ReservedFor_BraceCharacterInReference_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB :-) R571.54 reserved for purchase @ Some Random Merchant Char(1 from FNB card a/c..123000 using card..8765. Avail R3201. 23Jun 08:40");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(571.54, parsedEmail.Amount, 3);
        Assert.Equal("reserved for purchase", parsedEmail.Action);
        Assert.Equal("FNB card", parsedEmail.AccountType);
        Assert.Equal("123000", parsedEmail.AccountNumber);
        Assert.Equal("8765", parsedEmail.PartialCardNumber);
        Assert.Equal("card", parsedEmail.Method);
        Assert.Equal(3201, parsedEmail.Available ?? 0, 3);
        Assert.Equal("Some Random Merchant Char(1", parsedEmail.Reference);
        Assert.Equal("23Jun", parsedEmail.Date);
        Assert.Equal("08:40", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_ReservedFor_PeriodCharacterInReference_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB :-) R571.54 reserved for purchase @ Steamgames.com 4259522 from FNB card a/c..123000 using card..8765. Avail R3201. 23Jun 08:40");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(571.54, parsedEmail.Amount, 3);
        Assert.Equal("reserved for purchase", parsedEmail.Action);
        Assert.Equal("FNB card", parsedEmail.AccountType);
        Assert.Equal("123000", parsedEmail.AccountNumber);
        Assert.Equal("8765", parsedEmail.PartialCardNumber);
        Assert.Equal("card", parsedEmail.Method);
        Assert.Equal(3201, parsedEmail.Available ?? 0, 3);
        Assert.Equal("Steamgames.com 4259522", parsedEmail.Reference);
        Assert.Equal("23Jun", parsedEmail.Date);
        Assert.Equal("08:40", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_WithdrawnFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB:-) R1500.00 withdrawn from Premier a/c..12345 using card..3456 @ Cxb009650000001. Avail R54321. 15Jun 08:02");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(1500.00, parsedEmail.Amount, 3);
        Assert.Equal("withdrawn from", parsedEmail.Action);
        Assert.Equal("Premier", parsedEmail.AccountType);
        Assert.Equal("12345", parsedEmail.AccountNumber);
        Assert.Equal("3456", parsedEmail.PartialCardNumber);
        Assert.Equal("card", parsedEmail.Method);
        Assert.Equal(54321, parsedEmail.Available ?? 0, 3);
        Assert.Equal("Cxb009650000001", parsedEmail.Reference);
        Assert.Equal("15Jun", parsedEmail.Date);
        Assert.Equal("08:02", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_TransferFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB:-) R12345.00 t/fer from Premier a/c..765987 to FNB card a/c..514364 @ Online Banking. Avail R775533. 11Jun 09:28");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(12345.00, parsedEmail.Amount, 3);
        Assert.Equal("t/fer from", parsedEmail.Action);
        Assert.Equal("Premier", parsedEmail.AccountType);
        Assert.Equal("765987", parsedEmail.AccountNumber);
        Assert.Empty(parsedEmail.PartialCardNumber);
        Assert.Equal("Online Banking", parsedEmail.Method);
        Assert.Equal(775533, parsedEmail.Available ?? 0, 3);
        Assert.Equal("FNB card a/c..514364", parsedEmail.Reference);
        Assert.Equal("11Jun", parsedEmail.Date);
        Assert.Equal("09:28", parsedEmail.Time);
    }
}