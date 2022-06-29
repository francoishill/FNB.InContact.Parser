using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FNB.InContact.Parser.Tests.Services;

public class InContactHttpRequestExtractorTests
{
    [Fact]
    public async Task ExtractInContactLines_ExpectedBehaviour()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var logger = Substitute.For<ILogger>();
        var extractor = new InContactHttpRequestExtractor(logger);

        var emailContentStream = TestHelpers.GetTestDataFileContentStream("InContactEmails", "TwoHtmlEntries.txt");

        // Act
        var lines = (await extractor.ExtractInContactLines(emailContentStream, true, cancellationToken)).ToList();

        // Assert
        Assert.Equal(2, lines.Count);
        Assert.Equal("FNB:-) R98.00 paid from Premier a/c..123456 @ Eft. Avail R54321. Ref.Merchant one 92374623. 2Jun 00:00", lines[0]);
        Assert.Equal("FNB:-) R749.00 paid from Premier a/c..123456 @ Eft. Avail R54321. Ref.Merchant Two 29348723. 2Jun 00:00", lines[1]);
    }
}