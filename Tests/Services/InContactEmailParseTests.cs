using FNB.InContact.Parser.FunctionApp.Models.ValueObjects;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FNB.InContact.Parser.Tests.Services;

public class InContactEmailParseTests
{
    [Fact]
    public void ParseInContactLines_PaidTo_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB:-) R113355.20 paid to My Account Type a/c..112233 @ My Method. Ref.My Reference Name. 24Jun 00:00");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(TransactionDirection.Income, parsedEmail.Direction);
        Assert.Equal(113355.20, parsedEmail.Amount, 3);
        Assert.Equal("paid to", parsedEmail.Action);
        Assert.Equal("My Account Type", parsedEmail.AccountType);
        Assert.Equal("112233", parsedEmail.AccountNumber);
        Assert.Empty(parsedEmail.PartialCardNumber);
        Assert.Equal("My Method", parsedEmail.Method);
        Assert.Null(parsedEmail.Available);
        Assert.Equal("My Reference Name", parsedEmail.Reference);
        Assert.Equal("24Jun", parsedEmail.Date);
        Assert.Equal("00:00", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_ReversalOf_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB :-) REVERSAL of R113355.20 for Some Reference Name from My Account Type a/c..112233 using My Method..7531. 21Jun 11:56");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(TransactionDirection.Income, parsedEmail.Direction);
        Assert.Equal(113355.20, parsedEmail.Amount, 3);
        Assert.Equal("REVERSAL of", parsedEmail.Action);
        Assert.Equal("My Account Type", parsedEmail.AccountType);
        Assert.Equal("112233", parsedEmail.AccountNumber);
        Assert.Equal("7531", parsedEmail.PartialCardNumber);
        Assert.Equal("My Method", parsedEmail.Method);
        Assert.Null(parsedEmail.Available);
        Assert.Equal("Some Reference Name", parsedEmail.Reference);
        Assert.Equal("21Jun", parsedEmail.Date);
        Assert.Equal("11:56", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_PaidFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB:-) R113355.20 paid from My Account Type a/c..112233 @ My Method. Avail R12345. Ref.My Reference Name. 20Jun 03:27");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(TransactionDirection.Expense, parsedEmail.Direction);
        Assert.Equal(113355.20, parsedEmail.Amount, 3);
        Assert.Equal("paid from", parsedEmail.Action);
        Assert.Equal("My Account Type", parsedEmail.AccountType);
        Assert.Equal("112233", parsedEmail.AccountNumber);
        Assert.Empty(parsedEmail.PartialCardNumber);
        Assert.Equal("My Method", parsedEmail.Method);
        Assert.Equal(12345, parsedEmail.Available ?? 0, 3);
        Assert.Equal("My Reference Name", parsedEmail.Reference);
        Assert.Equal("20Jun", parsedEmail.Date);
        Assert.Equal("03:27", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_ReservedFor_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB :-) R113355.20 reserved for purchase @ My Reference Name from My Account Type a/c..112233 using My Method..7531. Avail R12345. 20Jun 03:27");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(TransactionDirection.Expense, parsedEmail.Direction);
        Assert.Equal(113355.20, parsedEmail.Amount, 3);
        Assert.Equal("reserved for purchase", parsedEmail.Action);
        Assert.Equal("My Account Type", parsedEmail.AccountType);
        Assert.Equal("112233", parsedEmail.AccountNumber);
        Assert.Equal("7531", parsedEmail.PartialCardNumber);
        Assert.Equal("My Method", parsedEmail.Method);
        Assert.Equal(12345, parsedEmail.Available ?? 0, 3);
        Assert.Equal("My Reference Name", parsedEmail.Reference);
        Assert.Equal("20Jun", parsedEmail.Date);
        Assert.Equal("03:27", parsedEmail.Time);
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

        Assert.Equal(TransactionDirection.Expense, parsedEmail.Direction);
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

        Assert.Equal(TransactionDirection.Expense, parsedEmail.Direction);
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
        var parsedEmail = parser.ParseInContactLines("FNB:-) R113355.20 withdrawn from My Account Type a/c..112233 using My Method..7531 @ My Reference Name. Avail R12345. 20Jun 03:27");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(TransactionDirection.Expense, parsedEmail.Direction);
        Assert.Equal(113355.20, parsedEmail.Amount, 3);
        Assert.Equal("withdrawn from", parsedEmail.Action);
        Assert.Equal("My Account Type", parsedEmail.AccountType);
        Assert.Equal("112233", parsedEmail.AccountNumber);
        Assert.Equal("7531", parsedEmail.PartialCardNumber);
        Assert.Equal("My Method", parsedEmail.Method);
        Assert.Equal(12345, parsedEmail.Available ?? 0, 3);
        Assert.Equal("My Reference Name", parsedEmail.Reference);
        Assert.Equal("20Jun", parsedEmail.Date);
        Assert.Equal("03:27", parsedEmail.Time);
    }

    [Fact]
    public void ParseInContactLines_TransferFrom_ExpectedBehaviour()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var parser = new InContactTextParser(logger);

        // Act
        var parsedEmail = parser.ParseInContactLines("FNB:-) R113355.20 t/fer from My Account Type a/c..112233 to FNB card a/c..345654 @ My Method. Avail R12345. 20Jun 03:27");

        // Assert
        Assert.NotNull(parsedEmail);

        Assert.Equal(TransactionDirection.Expense, parsedEmail.Direction);
        Assert.Equal(113355.20, parsedEmail.Amount, 3);
        Assert.Equal("t/fer from", parsedEmail.Action);
        Assert.Equal("My Account Type", parsedEmail.AccountType);
        Assert.Equal("112233", parsedEmail.AccountNumber);
        Assert.Empty(parsedEmail.PartialCardNumber);
        Assert.Equal("My Method", parsedEmail.Method);
        Assert.Equal(12345, parsedEmail.Available ?? 0, 3);
        Assert.Equal("FNB card a/c..345654", parsedEmail.Reference);
        Assert.Equal("20Jun", parsedEmail.Date);
        Assert.Equal("03:27", parsedEmail.Time);
    }
}