using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FNB.InContact.Parser.FunctionApp.Models.BusMessages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FNB.InContact.Parser.FunctionApp.Functions;

public static class ReceiveAndStoreSendGridEmail
{
    [FunctionName("ReceiveAndStoreSendGridEmail")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        [ServiceBus("%ReceivedSendGridEmailsQueueName%", EntityType = ServiceBusEntityType.Queue)] IAsyncCollector<ServiceBusMessage> receivedSendGridEmailsCollector,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        var busMessageJson = JsonConvert.SerializeObject(new ReceivedSendGridEmailBusMessage(
            requestBody,
            req.HasFormContentType,
            req.ContentType,
            req.Headers.ToDictionary(k => k.Key, v => v.Value.FirstOrDefault())));

        var busMessage = new ServiceBusMessage(busMessageJson)
        {
            CorrelationId = req.HttpContext.TraceIdentifier
        };

        await receivedSendGridEmailsCollector.AddAsync(busMessage, cancellationToken);

        return new OkResult();
    }
}