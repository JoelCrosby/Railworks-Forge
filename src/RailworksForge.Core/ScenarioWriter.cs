using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public static class ScenarioWriter
{
    // A distinct name so a Scenario.bin.xml the user exported next to the scenario isn't overwritten.
    private const string StagingXmlFilename = "Scenario.forge-staging.bin.xml";

    public static async Task WriteBinary(Scenario scenario, IDocument document)
    {
        Directory.CreateDirectory(scenario.DirectoryPath);

        var binDestination = Path.Join(scenario.DirectoryPath, "Scenario.bin");
        var stagingXmlPath = Path.Join(scenario.DirectoryPath, StagingXmlFilename);

        try
        {
            await document.ToXmlAsync(stagingXmlPath);

            var converted = await Serz.Convert(stagingXmlPath, force: true);

            File.Move(converted.OutputPath, binDestination, overwrite: true);
        }
        finally
        {
            File.Delete(stagingXmlPath);
        }

        await Paths.CreateMd5HashFile(binDestination);
    }

    public static async Task WritePropertiesDocument(Scenario scenario, IDocument document)
    {
        Directory.CreateDirectory(scenario.DirectoryPath);

        var destination = Path.Join(scenario.DirectoryPath, "ScenarioProperties.xml");
        var stagingPath = $"{destination}.forge-staging";

        await document.ToXmlAsync(stagingPath);

        File.Move(stagingPath, destination, overwrite: true);

        await Paths.CreateMd5HashFile(destination);
    }
}
