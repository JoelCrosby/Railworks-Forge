using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using Serilog;

namespace RailworksForge.Core.Commands.Common;

public static class ServiceVehicleUpdates
{
    public static void AddRequiredBlueprintSet(IDocument scenarioPropertiesDocument, Blueprint blueprint)
    {
        scenarioPropertiesDocument.UpdateBlueprintSetCollection(blueprint, "RBlueprintSetPreLoad");
        scenarioPropertiesDocument.UpdateBlueprintSetCollection(blueprint, "RequiredSet");
    }

    public static void UpdateLeadVehicle(IDocument scenarioPropertiesDocument, Consist consist, RollingStockEntry vehicle)
    {
        var serviceElement = scenarioPropertiesDocument
            .QuerySelectorAll("sDriverFrontEndDetails")
            .QueryByTextContent("ServiceName Key", consist.ServiceId);

        if (serviceElement is null)
        {
            Log.Warning("Could not find service {Service} in scenario properties file", consist.ServiceName);
            return;
        }

        UpdateLeadBlueprint(serviceElement, vehicle);
        UpdateLeadFilePath(serviceElement, vehicle);
    }

    public static void AppendInitialVehicleNumber(IDocument scenarioDocument, IElement serviceConsist, string number)
    {
        if (serviceConsist.QuerySelector("Driver cDriver InitialRV") is not { } initialRv)
        {
            return;
        }

        var entry = scenarioDocument.CreateXmlElement("e");
        entry.SetAttribute(Utilities.NS, "d:type", "cDeltaString");
        entry.SetTextContent(number);

        initialRv.AppendChild(entry);
    }

    // Numbers aren't unique (wagons often share one), so only a single entry in the edited consist is touched.
    public static void RemoveInitialVehicleNumber(IElement serviceConsist, string number)
    {
        FindInitialVehicleNumber(serviceConsist, number)?.Remove();
    }

    public static void RenameInitialVehicleNumber(IElement serviceConsist, string previousNumber, string number)
    {
        FindInitialVehicleNumber(serviceConsist, previousNumber)?.SetTextContent(number);
    }

    // Instructions target vehicles by number and can live on any driver (e.g. a loco collecting wagons from a static
    // consist), so they are renamed across the scenario — but only when no other vehicle still has the old number.
    public static void RenameInstructionTargets(IDocument scenarioDocument, string previousNumber, string number)
    {
        var vehiclesWithPreviousNumber = scenarioDocument
            .QuerySelectorAll("RailVehicles cOwnedEntity UniqueNumber")
            .Count(element => element.TextContent == previousNumber);

        if (vehiclesWithPreviousNumber > 0)
        {
            return;
        }

        var instructionTargets = scenarioDocument
            .QuerySelectorAll("cDriverInstructionTarget RailVehicleNumber e")
            .Where(entry => entry.TextContent == previousNumber)
            .ToList();

        foreach (var entry in instructionTargets)
        {
            entry.SetTextContent(number);
        }
    }

    private static IElement? FindInitialVehicleNumber(IElement serviceConsist, string number)
    {
        return serviceConsist
            .QuerySelectorAll("Driver cDriver InitialRV e")
            .FirstOrDefault(entry => entry.TextContent == number);
    }

    private static void UpdateLeadBlueprint(IElement serviceElement, RollingStockEntry vehicle)
    {
        serviceElement.UpdateTextElement("LocoName Key", Guid.NewGuid().ToString());
        serviceElement.UpdateTextElement("LocoName English", vehicle.LocomotiveName);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID", vehicle.Blueprint.BlueprintId);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider", vehicle.Blueprint.BlueprintSetIdProvider);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product", vehicle.Blueprint.BlueprintSetIdProduct);
        serviceElement.UpdateTextElement("LocoAuthor", vehicle.Blueprint.BlueprintSetIdProvider);
    }

    private static void UpdateLeadFilePath(IElement serviceElement, RollingStockEntry vehicle)
    {
        if (serviceElement.QuerySelector("FilePath") is not { } filePath)
        {
            return;
        }

        var parts = vehicle.Blueprint.BlueprintId.Split('\\');
        var partsWithoutFilename = parts[..^1];
        var blueprintDirectory = string.Join('\\', partsWithoutFilename);
        var packagedPath = $@"{vehicle.Blueprint.BlueprintSetIdProvider}\{vehicle.Blueprint.BlueprintSetIdProduct}\{blueprintDirectory}";

        filePath.SetTextContent(packagedPath);
    }
}
