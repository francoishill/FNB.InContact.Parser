﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Factories;
using FNB.InContact.Parser.FunctionApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace FNB.InContact.Parser.FunctionApp.Functions;

public static class GetReportForDateRange
{
    [FunctionName("GetReportForDateRange")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log,
        [Table("BankReferenceToCategoryMappings")] CloudTable bankReferenceMappingsTable,
        [Table("ParsedInContactTextLines")] CloudTable parsedEntitiesTable,
        [Table("NonParsedInContactTextLines")] CloudTable nonParsedEntitiesTable,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var startDateString = req.Query["startDate"];
        if (!DateTime.TryParse(startDateString, out var startDate))
        {
            return HttpResponseFactory.CreateBadRequestResponse("startDate query parameter is required and must be a valid date");
        }

        var endDateString = req.Query["endDate"];
        if (!DateTime.TryParse(endDateString, out var endDate))
        {
            return HttpResponseFactory.CreateBadRequestResponse("endDate query parameter is required and must be a valid date");
        }

        if (startDate >= endDate)
        {
            return HttpResponseFactory.CreateBadRequestResponse("Start date must be before end date");
        }

        var reportBuilder = new ReportBuilderService();

        var reportResult = await reportBuilder.GenerateHtmlReport(
            bankReferenceMappingsTable,
            parsedEntitiesTable,
            nonParsedEntitiesTable,
            startDate,
            endDate,
            cancellationToken);

        return new ContentResult
        {
            StatusCode = (int)HttpStatusCode.OK,
            Content = reportResult.Body,
            ContentType = "text/html",
        };
    }
}