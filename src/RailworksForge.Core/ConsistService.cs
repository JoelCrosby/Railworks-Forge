using System.Diagnostics;

using AngleSharp;
using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistService
{
    public static async Task ReplaceConsist(Consist target, PreloadConsist preload, Scenario scenario)
    {
        var scenarioDocument = await scenario.GetXmlDocument();

        var scenarioConsist = scenarioDocument.QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("Driver ServiceName Key") == target.ServiceId);

        if (scenarioConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        var blueprintNodes = scenarioConsist.QuerySelectorAll("RailVehicles cOwnedEntity");
        var blueprintText = blueprintNodes.Select(n => n.SelectTextContnet("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID")).ToList();

        Debug.WriteLine("found blueprint node count {0}", blueprintText.Count);

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

        var filename = $"scenario-{DateTimeOffset.UtcNow:yy-MMM-dd-ddd-hh-mm}.xml";
        var outputPath = Path.Join(Paths.GetHomeDirectory(), "Downloads", filename);

        await using var stream = File.OpenWrite(outputPath);
        await scenarioDocument.ToHtmlAsync(stream);
    }
}
