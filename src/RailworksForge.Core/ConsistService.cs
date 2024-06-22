using AngleSharp.Dom;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistService
{


    public static async Task ReplaceConsist(Consist target, PreloadConsist preload, Scenario scenario)
    {
        var directory = $"{scenario.Name.ToUrlSlug()}-{DateTimeOffset.UtcNow:yy-MMM-dd-ddd-hh-mm}";
        var outputDirectory = Path.Join(Paths.GetHomeDirectory(), "Downloads", directory);

        Directory.CreateDirectory(outputDirectory);

        try
        {
            var scenarioDocument = await GetUpdatedScenario(scenario, target, preload);
            var scenarioPropertiesDocument = await GetUpdatedScenarioProperties(scenario, target, preload);

            await WriteScenarioDocument(outputDirectory, scenarioDocument);
            await WriteScenarioPropertiesDocument(outputDirectory, scenarioPropertiesDocument);
        }
        catch
        {
            Directory.Delete(outputDirectory);
        }
    }

    private static async Task<IXmlDocument> GetUpdatedScenario(Scenario scenario, Consist target, PreloadConsist preload)
    {
        var document = await scenario.GetXmlDocument();

        var scenarioConsist = document
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("Driver ServiceName Key", target.ServiceId);

        if (scenarioConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        var blueprintNodes = scenarioConsist.QuerySelectorAll("RailVehicles cOwnedEntity");
        var nodeCountToKeep = preload.ConsistEntries.Count;

        await Parallel.ForEachAsync(preload.ConsistEntries, async (entry, _) => await entry.GetXmlDocument());

        for (var i = 0; i < blueprintNodes.Length; i++)
        {
            var scenarioNode = blueprintNodes[i];

            if (i >= nodeCountToKeep)
            {
                scenarioNode.RemoveFromParent();
                continue;
            }

            var blueprintNode = preload.ConsistEntries[i];
            var blueprintBinDocument = await blueprintNode.GetXmlDocument();

            var blueprintName = blueprintBinDocument.SelectTextContent("Blueprint Name");

            var name = scenarioNode.QuerySelector("Name");
            var blueprint = scenarioNode.QuerySelector("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID");

            if (blueprint is null) continue;

            var scenarioBlueprintId = blueprint.QuerySelector("BlueprintID");
            var scenarioProvider = blueprint.QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider");
            var scenarioProduct = blueprint.QuerySelector("BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product");


            if (scenarioProvider is null || scenarioProduct is null || scenarioBlueprintId is null)
            {
                continue;
            }

            if (name is not null)
            {
                name.TextContent = blueprintName;
            }

            scenarioProvider.TextContent = blueprintNode.BlueprintIdProvider;
            scenarioProduct.TextContent = blueprintNode.BlueprintIdProduct;
            scenarioBlueprintId.TextContent = blueprintNode.BlueprintId;
        }

        return document;
    }

    private static async Task<IXmlDocument> GetUpdatedScenarioProperties(Scenario scenario, Consist target, PreloadConsist preload)
    {
        var document = await scenario.GetPropertiesXmlDocument();

        var serviceElement = document
            .QuerySelectorAll("sDriverFrontEndDetails")
            .QueryByTextContent("ServiceName Key", target.ServiceId);

        if (serviceElement is null)
        {
            throw new Exception($"could not find service {target.ServiceName} in scenario properties file.");
        }

        if (serviceElement.QuerySelector("LocoName English") is {} locoName)
        {
            locoName.TextContent = preload.LocomotiveName;
        }

        if (serviceElement.QuerySelector("LocoBP BlueprintID") is {} blueprintId)
        {
            blueprintId.TextContent = preload.BlueprintId;
        }

        if (serviceElement.QuerySelector("LocoBP Provider") is {} blueprintProviderId)
        {
            blueprintProviderId.TextContent = preload.BlueprintIdProvider;
        }

        if (serviceElement.QuerySelector("LocoBP Product") is {} blueprintProductId)
        {
            blueprintProductId.TextContent = preload.BlueprintIdProduct;
        }

        if (serviceElement.QuerySelector("FilePath") is {} filePath)
        {
            var parts = preload.BlueprintId.Split('\\');
            var partsWithoutFilename = parts[..^1];
            var blueprintDirectory = string.Join('\\', partsWithoutFilename);
            var packagedPath = $@"{preload.BlueprintIdProvider}\{preload.BlueprintIdProduct}\{blueprintDirectory}";

            filePath.TextContent = packagedPath;
        }

        return document;
    }

    private static async Task WriteScenarioDocument(string path, IXmlDocument document)
    {
        const string filename = "Scenario.bin.xml";
        var outputPath = Path.Join(path, filename);

        await using var stream = File.OpenWrite(outputPath);
        await document.ToXmlAsync(stream);

        await Serz.Convert(outputPath);
    }

    private static async Task WriteScenarioPropertiesDocument(string path, IXmlDocument document)
    {
        const string filename = "ScenarioProperties.xml";
        var outputPath = Path.Join(path, filename);

        await using var stream = File.OpenWrite(outputPath);
        await document.ToXmlAsync(stream);
    }
}
