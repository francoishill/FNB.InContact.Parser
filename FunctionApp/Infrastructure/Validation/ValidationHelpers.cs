﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FNB.InContact.Parser.FunctionApp.Infrastructure.Validation;

public static class ValidationHelpers
{
    public static bool ValidateDataAnnotations(object obj, out List<ValidationResult> results)
    {
        var ctx = new ValidationContext(obj);

        results = new List<ValidationResult>();

        return Validator.TryValidateObject(obj, ctx, results, true);
    }
}