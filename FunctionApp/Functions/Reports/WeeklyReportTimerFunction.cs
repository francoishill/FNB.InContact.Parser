using System;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace FNB.InContact.Parser.FunctionApp.Functions.Reports;

public static class WeeklyReportTimerFunction
{
    [FunctionName("WeeklyReportTimerFunction")]
    public static async Task RunAsync(
        [TimerTrigger("0 0 13 * * SAT")] TimerInfo myTimer,
        ILogger log,
        [Table("BankReferenceToCategoryMappings")] CloudTable bankReferenceMappingsTable,
        [Table("ParsedInContactTextLines")] CloudTable parsedEntitiesTable,
        [Table("NonParsedInContactTextLines")] CloudTable nonParsedEntitiesTable,
        [SendGrid(ApiKey = "SendGridApiKey", From = "%FromEmailAddress%", To = "%ToEmailAddress%")] IAsyncCollector<SendGridMessage> emailMessageCollector,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# Timer trigger function executed at: {Date}", DateTime.UtcNow);

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-7);

        var reportBuilder = new ReportBuilderService();

        var reportResult = await reportBuilder.GenerateHtmlReport(
            bankReferenceMappingsTable,
            parsedEntitiesTable,
            nonParsedEntitiesTable,
            startDate,
            endDate,
            cancellationToken);

        var message = new SendGridMessage();

        message.AddContent("text/html", reportResult.Body);
        message.SetSubject(reportResult.Subject);

        await emailMessageCollector.AddAsync(message, cancellationToken);
    }
}