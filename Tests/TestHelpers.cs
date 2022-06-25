namespace FNB.InContact.Parser.Tests;

public static class TestHelpers
{
    public static async Task<string> GetTestDataFileContentString(string testDataSubdirDotsInsteadOfSlashes, string pathRelativeToTestData)
    {
        var resourceStream = GetTestDataFileContentStream(testDataSubdirDotsInsteadOfSlashes, pathRelativeToTestData);
        using var streamReader = new StreamReader(resourceStream);
        return await streamReader.ReadToEndAsync();
    }

    public static Stream GetTestDataFileContentStream(string testDataSubdirDotsInsteadOfSlashes, string pathRelativeToTestData)
    {
        if (testDataSubdirDotsInsteadOfSlashes.Contains("/") || testDataSubdirDotsInsteadOfSlashes.Contains("\\"))
        {
            throw new Exception($"{nameof(testDataSubdirDotsInsteadOfSlashes)} should contain . instead of slashes");
        }

        var assembly = typeof(TestHelpers).Assembly;
        var namespaceName = typeof(TestHelpers).Namespace;
        var fileFullName = $"{namespaceName}.TestData.{testDataSubdirDotsInsteadOfSlashes}." + pathRelativeToTestData.TrimStart('\\').Replace("\\", ".");
        var resourceStream = assembly.GetManifestResourceStream(fileFullName);

        if (resourceStream == null)
        {
            throw new Exception($"Unable to read file '{fileFullName}'");
        }

        return resourceStream;
    }
}