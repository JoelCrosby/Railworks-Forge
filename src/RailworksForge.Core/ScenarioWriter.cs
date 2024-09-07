using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public static class ScenarioWriter
{
    public static async Task WriteBinary(Scenario scenario, IDocument document)
    {
        const string filename = "Scenario.bin.xml";
        const string binFilename = "Scenario.bin";

        var destination = GetScenarioPathForFilename(scenario, filename);
        var binDestination = GetScenarioPathForFilename(scenario, binFilename);

        File.Delete(destination);

        await document.ToXmlAsync(destination);

        File.Delete(binDestination);

        var converted = await Serz.Convert(destination, true);

        File.Copy(converted.OutputPath, binDestination);

        File.Delete(converted.OutputPath);
        File.Delete(destination);

        await Paths.CreateMd5HashFile(binDestination);
    }

    public static async Task WritePropertiesDocument(Scenario scenario, IDocument document)
    {
        const string filename = "ScenarioProperties.xml";

        var destination = GetScenarioPathForFilename(scenario, filename);

        File.Delete(destination);

        await document.ToXmlAsync(destination);
        await Paths.CreateMd5HashFile(destination);
    }

    private static string GetScenarioPathForFilename(Scenario scenario, string filename)
    {
        return scenario.PackagingType is PackagingType.Unpacked
            ? Path.Join(scenario.DirectoryPath, filename)
            : Path.Join(scenario.DirectoryPath, "Scenarios", scenario.Id, filename);
    }
}
