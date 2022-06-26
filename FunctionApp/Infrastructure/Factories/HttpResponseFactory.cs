using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace FNB.InContact.Parser.FunctionApp.Infrastructure.Factories;

public class HttpResponseFactory
{
    public static IActionResult CreateBadRequestResponse(params string[] errors)
    {
        return new BadRequestObjectResult(new Dictionary<string, object>
        {
            { "Errors", errors }
        });
    }
}