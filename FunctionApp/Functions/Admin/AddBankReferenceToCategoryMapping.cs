using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Factories;
using FNB.InContact.Parser.FunctionApp.Infrastructure.Validation;
using FNB.InContact.Parser.FunctionApp.Models.TableEntities;
using FNB.InContact.Parser.FunctionApp.Models.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable UnusedMember.Local
// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CollectionNeverUpdated.Local

namespace FNB.InContact.Parser.FunctionApp.Functions.Admin;

public static class AddBankReferenceToCategoryMapping
{
    [FunctionName("AddBankReferenceToCategoryMapping")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        [Table("BankReferenceToCategoryMappings")] CloudTable mappingsTable,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestDTO = JsonConvert.DeserializeObject<RequestDto>(requestBody);
        if (!ValidationHelpers.ValidateDataAnnotations(requestDTO, out var validationResultsForRequest))
        {
            return HttpResponseFactory.CreateBadRequestResponse(new[] { "Request body is invalid" }.Concat(validationResultsForRequest.Select(s => s.ErrorMessage)).ToArray());
        }

        var mappingValidationErrors = new List<string>();
        foreach (var mapping in requestDTO.Mappings)
        {
            if (!ValidationHelpers.ValidateDataAnnotations(mapping, out var validationResultsForMapping))
            {
                mappingValidationErrors.AddRange(new[] { $"Mapping is invalid: {JsonConvert.SerializeObject(mapping)}" }.Concat(validationResultsForMapping.Select(s => s.ErrorMessage)).ToArray());
            }
        }

        if (mappingValidationErrors.Count > 0)
        {
            return HttpResponseFactory.CreateBadRequestResponse(mappingValidationErrors.ToArray());
        }

        var response = new ResponseDto
        {
            SuccessfullyAddedCount = 0,
            AlreadyExistingEntities = new List<RequestDto.BankReferenceToCategoryMapping>(),
            FailedEntities = new List<RequestDto.BankReferenceToCategoryMapping>(),
        };

        foreach (var mapping in requestDTO.Mappings)
        {
            if (mapping.Direction == null)
            {
                return HttpResponseFactory.CreateBadRequestResponse($"Direction cannot be NULL: {JsonConvert.SerializeObject(mapping)}");
            }

            var entity = new BankReferenceToCategoryMappingEntity(
                mapping.Direction.Value,
                mapping.BankReferenceRegexPattern,
                mapping.CategoryName);

            try
            {
                await mappingsTable.ExecuteAsync(TableOperation.InsertOrReplace(entity), cancellationToken);
                response.SuccessfullyAddedCount++;
            }
            catch (StorageException storageException) when (storageException.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
            {
                response.AlreadyExistingEntities.Add(mapping);
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Unable to store mapping, error was {Error}, mapping is {Mapping}", exception.Message, JsonConvert.SerializeObject(mapping));
                response.FailedEntities.Add(mapping);
            }
        }

        if (response.SuccessfullyAddedCount != requestDTO.Mappings.Count)
        {
            return new BadRequestObjectResult(response);
        }

        return new OkObjectResult(response);
    }

    private class RequestDto
    {
        [Required, MinLength(1)]
        public List<BankReferenceToCategoryMapping> Mappings { get; set; }

        public class BankReferenceToCategoryMapping
        {
            [Required]
            public TransactionDirection? Direction { get; set; }

            [Required, MinLength(2)]
            public string BankReferenceRegexPattern { get; set; }

            [Required, MinLength(2)]
            public string CategoryName { get; set; }
        }
    }

    private class ResponseDto
    {
        public int SuccessfullyAddedCount { get; set; }
        public int AlreadyExistingEntitiesCount => AlreadyExistingEntities.Count;
        public int NonSavedEntitiesCount => FailedEntities.Count;
        public List<RequestDto.BankReferenceToCategoryMapping> AlreadyExistingEntities { get; set; }
        public List<RequestDto.BankReferenceToCategoryMapping> FailedEntities { get; set; }
    }
}