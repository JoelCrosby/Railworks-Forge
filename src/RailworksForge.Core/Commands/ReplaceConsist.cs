using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.models;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using Serilog;

namespace RailworksForge.Core.Commands;

public class ReplaceConsist : IConsistCommand
{
    private readonly ReplaceConsistRequest _request;

    public ReplaceConsist(ReplaceConsistRequest request)
    {
        _request = request;
    }

    public async Task Run(ConsistCommandContext context)
    {
        await GetUpdatedScenario(context);

        GetUpdatedScenarioProperties(context);
    }

    private async Task GetUpdatedScenario(ConsistCommandContext context)
    {
        var document = context.ScenarioDocument;

        var target = _request.Target;
        var preload = _request.PreloadConsist;

        foreach (var consist in target.GetConsists())
        {
            var serviceConsist = GetServiceConsist(document, consist);

            if (serviceConsist is null)
            {
                throw new Exception("unable to find scenario consist");
            }

            var railVehicles = serviceConsist.QuerySelector("RailVehicles");

            if (railVehicles is null)
            {
                Log.Warning("unable to find rail vehicles in scenario document");
                continue;
            }

            var blueprintNodes = serviceConsist.QuerySelectorAll("RailVehicles cOwnedEntity");

            if (blueprintNodes.FirstOrDefault()?.Clone() is not IElement firstBlueprint)
            {
                throw new Exception("unable to clone first blueprint");
            }

            await Parallel.ForEachAsync(preload.ConsistEntries, async (entry, _) => await entry.GetXmlDocument());

            var selectedConsistLength = preload.ConsistEntries.Count;

            foreach (var node in blueprintNodes)
            {
                node.RemoveFromParent();
            }

            var cDriver = serviceConsist.QuerySelector("Driver cDriver");

            var previousVehicle = firstBlueprint;
            var initialRv = cDriver?.QuerySelector("InitialRV");

            if (initialRv is not null)
            {
                foreach (var child in initialRv.Children)
                {
                    child.RemoveFromParent();
                }
            }

            for (var i = 0; i < selectedConsistLength; i++)
            {
                var consistVehicle = preload.ConsistEntries[i];
                var railVehicle = await AddConsistVehicle(consistVehicle, document, previousVehicle);

                if (railVehicle is null)
                {
                    throw new Exception("failed to create rail vehicle for consist");
                }

                var e = document.CreateXmlElement("e");
                e.SetAttribute(Utilities.NS, "d:type", "cDeltaString");
                e.SetTextContent(railVehicle.Number);

                initialRv?.AppendChild(e);

                var element = railVehicle.Element;

                if (element is null)
                {
                    throw new Exception("could not find body in rail vehicle element");
                }

                railVehicles.AppendChild(element);
                previousVehicle = element;
            }
        }
    }

    private static IElement? GetServiceConsist(IDocument document, Consist consist)
    {
        return document.QuerySelectorAll("cConsist").FirstOrDefault(el => el.GetAttribute("d:id") == consist.Id);
    }

    private void GetUpdatedScenarioProperties(ConsistCommandContext context)
    {
        var document = context.ScenarioPropertiesDocument;

        var target = _request.Target;
        var preload = _request.PreloadConsist;

        foreach (var consist in target.GetConsists())
        {
            var serviceElement = document
                .QuerySelectorAll("sDriverFrontEndDetails")
                .QueryByTextContent("ServiceName Key", consist.ServiceId);

            if (serviceElement is null)
            {
                Log.Warning("could not find service {Service} in scenario properties file", consist.ServiceName);
                continue;
            }

            UpdateBlueprint(serviceElement, preload);
            UpdateFilePath(serviceElement, preload);

            foreach (var entry in preload.ConsistEntries)
            {
                UpdateBlueprintElements(document, entry.Blueprint);
            }
        }
    }

    private static void UpdateBlueprint(IElement serviceElement, PreloadConsist preload)
    {
        serviceElement.UpdateTextElement("LocoName Key", Guid.NewGuid().ToString());
        serviceElement.UpdateTextElement("LocoName English", preload.LocomotiveName);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID", preload.Blueprint.BlueprintId);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider", preload.Blueprint.BlueprintSetIdProvider);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product", preload.Blueprint.BlueprintSetIdProduct);
        serviceElement.UpdateTextElement("LocoClass", LocoClassUtils.ToLongFormString(preload.EngineType));
        serviceElement.UpdateTextElement("LocoAuthor", preload.Blueprint.BlueprintSetIdProvider);
    }

    private static void UpdateFilePath(IElement serviceElement, PreloadConsist preload)
    {
        if (serviceElement.QuerySelector("FilePath") is not { } filePath)
        {
            return;
        }

        var parts = preload.Blueprint.BlueprintId.Split('\\');
        var partsWithoutFilename = parts[..^1];
        var blueprintDirectory = string.Join('\\', partsWithoutFilename);
        var packagedPath = $@"{preload.Blueprint.BlueprintSetIdProvider}\{preload.Blueprint.BlueprintSetIdProduct}\{blueprintDirectory}";

        filePath.SetTextContent(packagedPath);
    }

    private static void UpdateBlueprintElements(IDocument document, Blueprint blueprint)
    {
        document.UpdateBlueprintSetCollection(blueprint, "RBlueprintSetPreLoad");
        document.UpdateBlueprintSetCollection(blueprint, "RequiredSet");
    }

    private static async Task<GeneratedVehicle> AddConsistVehicle(ConsistEntry consistVehicle, IDocument document, IElement scenarioNode)
    {
        var vehicleDocument = await consistVehicle.GetXmlDocument();
        var scenarioConsist = ScenarioConsist.ParseConsist(vehicleDocument, consistVehicle);

        return await VehicleGenerator.GenerateVehicle(document, scenarioNode, scenarioConsist, consistVehicle);
    }
}
