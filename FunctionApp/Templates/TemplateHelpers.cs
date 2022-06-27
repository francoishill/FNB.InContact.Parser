using System;
using System.IO;
using System.Threading.Tasks;

namespace FNB.InContact.Parser.FunctionApp.Templates;

public static class TemplateHelpers
{
    public static async Task<string> GetHtmlTemplateStringAsync(string templateFileName)
    {
        var resourceStream = GetHtmlTemplateStream(templateFileName);
        using var streamReader = new StreamReader(resourceStream);
        return await streamReader.ReadToEndAsync();
    }

    private static Stream GetHtmlTemplateStream(string templateFileName)
    {
        var assembly = typeof(TemplateHelpers).Assembly;
        var namespaceName = typeof(TemplateHelpers).Namespace;
        var fileFullName = $"{namespaceName}.HtmlTemplates.{templateFileName}";
        var resourceStream = assembly.GetManifestResourceStream(fileFullName);

        if (resourceStream == null)
        {
            throw new Exception($"Unable to read file '{fileFullName}'");
        }

        return resourceStream;
    }
}