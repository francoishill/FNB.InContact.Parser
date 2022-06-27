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
        [Table("ParsedInContactTextLines")] CloudTable parsedEntitiesTable,
        [Table("NonParsedInContactTextLines")] CloudTable nonParsedEntitiesTable,
        [SendGrid(ApiKey = "SendGridApiKey", From = "%FromEmailAddress%", To = "%ToEmailAddress%")] IAsyncCollector<SendGridMessage> emailMessageCollector,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# Timer trigger function executed at: {Date}", DateTime.UtcNow);

        var functionAppBaseUrl = Environment.GetEnvironmentVariable("FunctionAppBaseUrl");
        if (string.IsNullOrWhiteSpace(functionAppBaseUrl))
        {
            log.LogCritical("Environment variable FunctionAppBaseUrl is required but empty");
            throw new Exception("Environment variable FunctionAppBaseUrl is required but empty");
        }

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-7);

        var reportBuilder = new ReportBuilderService();

        var emailSubject = await reportBuilder.GenerateReportEmailSubject(
            parsedEntitiesTable,
            nonParsedEntitiesTable,
            startDate,
            endDate,
            cancellationToken);

        var startDateString = startDate.ToString("O");
        var endDateString = endDate.ToString("O");
        var linkToReport = functionAppBaseUrl.TrimEnd('/') + $"/api/{nameof(GetReportForDateRange)}?startDate={startDateString}&endDate={endDateString}";
        var linkHtml = $"<a href=\"{linkToReport}\">{linkToReport}</a>";

        var message = new SendGridMessage();

        message.AddContent("text/html", $"View the report here:<br>{linkHtml}");
        message.SetSubject(emailSubject);

        await emailMessageCollector.AddAsync(message, cancellationToken);
    }
}