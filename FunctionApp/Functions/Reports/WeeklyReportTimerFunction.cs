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

        var functionAppBaseUrl = GetRequiredEnvironmentVariable(log, "FunctionAppBaseUrl");
        var functionAppReportSecretCode = GetRequiredEnvironmentVariable(log, "FunctionAppReportSecretCode");

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
        var linkToReport = functionAppBaseUrl.TrimEnd('/') + $"/api/{nameof(GetReportForDateRange)}?startDate={startDateString}&endDate={endDateString}&code={functionAppReportSecretCode}";
        var linkHtml = $"<a href=\"{linkToReport}\">{linkToReport}</a>";

        var message = new SendGridMessage();

        message.AddContent("text/html", $"View the report here:<br>{linkHtml}");
        message.SetSubject(emailSubject);

        await emailMessageCollector.AddAsync(message, cancellationToken);
    }

    private static string GetRequiredEnvironmentVariable(ILogger log, string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            log.LogCritical("Environment variable {Key} is required but empty", key);
            throw new Exception($"Environment variable {key} is required but empty");
        }

        return value;
    }
}