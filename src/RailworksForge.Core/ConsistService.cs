using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistService
{
    public static async Task ReplaceConsist(Consist target, PreloadConsist preload, Scenario scenario)
    {
        var document = await scenario.GetXmlDocument();

        var scenarioConsist = document.QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("Driver ServiceName Key") == target.ServiceId);

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

            var blueprintName = blueprintBinDocument.SelectTextContnet("Blueprint Name");

            var name = scenarioNode.QuerySelector("Name");
            var scenarioBlueprint = scenarioNode.QuerySelector("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID");

            if (scenarioBlueprint is null) continue;

            var scenarioBlueprintSetId = scenarioBlueprint.QuerySelector("BlueprintSetID");
            var scenarioProvider = scenarioBlueprint.QuerySelector("BlueprintSetID Provider");
            var scenarioProduct = scenarioBlueprint.QuerySelector("BlueprintSetID Product");


            if (scenarioProvider is null || scenarioProduct is null || scenarioBlueprintSetId is null)
            {
                continue;
            }

            if (name is not null)
            {
                name.TextContent = blueprintName;
            }

            scenarioProvider.TextContent = blueprintNode.BlueprintIdProvider;
            scenarioProduct.TextContent = blueprintNode.BlueprintIdProduct;
            scenarioBlueprintSetId.TextContent = blueprintNode.BlueprintId;
        }

        await UpdateScenarioProperties(scenario, target, preload);
        await WriteScenarioDocument(document);
    }

    private static async Task UpdateScenarioProperties(Scenario scenario, Consist target, PreloadConsist preload)
    {
        var document = await scenario.GetPropertiesXmlDocument();

        var serviceElement = document
            .QuerySelectorAll("sDriverFrontEndDetails")
            .FirstOrDefault(el => el.SelectTextContnet("ServiceName Key") == target.ServiceId);

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

        await WriteScenarioPropertiesDocument(document);
    }

    private static async Task WriteScenarioDocument(IXmlDocument document)
    {
        var filename = $"scenario-{DateTimeOffset.UtcNow:yy-MMM-dd-ddd-hh-mm}.xml";
        var outputPath = Path.Join(Paths.GetHomeDirectory(), "Downloads", filename);

        await using var stream = File.OpenWrite(outputPath);
        await document.ToHtmlAsync(stream);
    }

    private static async Task WriteScenarioPropertiesDocument(IXmlDocument document)
    {
        var filename = $"scenario-properties-{DateTimeOffset.UtcNow:yy-MMM-dd-ddd-hh-mm}.xml";
        var outputPath = Path.Join(Paths.GetHomeDirectory(), "Downloads", filename);

        await using var stream = File.OpenWrite(outputPath);
        await document.ToHtmlAsync(stream);
    }
}
