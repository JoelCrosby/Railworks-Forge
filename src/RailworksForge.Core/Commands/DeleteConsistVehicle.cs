using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using Serilog;

namespace RailworksForge.Core.Commands;

public class DeleteConsistVehicle : IConsistCommand
{
    private readonly DeleteConsistVehicleRequest _request;

    public DeleteConsistVehicle(DeleteConsistVehicleRequest request)
    {
        _request = request;
    }

    public async Task Run(ConsistCommandContext context)
    {
        await UpdatedScenario(context);
    }

    private async Task UpdatedScenario(ConsistCommandContext context)
    {
        var document = context.ScenarioDocument;

        var serviceConsist = Consist.GetServiceConsist(document, _request.Consist);

        if (serviceConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        var railVehicle = serviceConsist.QuerySelector($"RailVehicles cOwnedEntity[id='{_request.VehicleToDelete.Id}']");

        if (railVehicle is null)
        {
            throw new Exception("unable to find rail vehicle in scenario document");
        }

        railVehicle.Remove();

        if (railVehicle.Index() is 0)
        {
            var entry = serviceConsist.QuerySelector("RailVehicles")?.FirstElementChild;

            if (entry is null) return;

            var blueprint = new Blueprint
            {
                BlueprintSetIdProvider = entry.SelectTextContent("Provider"),
                BlueprintSetIdProduct = entry.SelectTextContent("Product"),
                BlueprintId = entry.SelectTextContent("BlueprintID"),
            };

            var vehicleDocument = await blueprint.GetXmlDocument();
            var vehicleElement = vehicleDocument.DocumentElement;
            var consistVehicle = RollingStockEntry.Parse(vehicleElement, blueprint);

            UpdateScenarioProperties(context, consistVehicle);
        }
    }

    private void UpdateScenarioProperties(ConsistCommandContext context, RollingStockEntry vehicle)
    {
        var document = context.ScenarioPropertiesDocument;
        var consist = _request.Consist;

        var serviceElement = document
            .QuerySelectorAll("sDriverFrontEndDetails")
            .QueryByTextContent("ServiceName Key", _request.Consist.ServiceId);

        if (serviceElement is null)
        {
            Log.Warning("Could not find service {Service} in scenario properties file", consist.ServiceName);
            return;
        }

        UpdateBlueprint(serviceElement, vehicle);
        UpdateFilePath(serviceElement, vehicle);
    }

    private static void UpdateBlueprint(IElement serviceElement, RollingStockEntry vehicle)
    {
        serviceElement.UpdateTextElement("LocoName Key", Guid.NewGuid().ToString());
        serviceElement.UpdateTextElement("LocoName English", vehicle.LocomotiveName);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID", vehicle.Blueprint.BlueprintId);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Provider", vehicle.Blueprint.BlueprintSetIdProvider);
        serviceElement.UpdateTextElement("LocoBP iBlueprintLibrary-cAbsoluteBlueprintID BlueprintSetID iBlueprintLibrary-cBlueprintSetID Product", vehicle.Blueprint.BlueprintSetIdProduct);
        // serviceElement.UpdateTextElement("LocoClass", LocoClassUtils.ToLongFormString(vehicle.BlueprintType));
        serviceElement.UpdateTextElement("LocoAuthor", vehicle.Blueprint.BlueprintSetIdProvider);
    }

    private static void UpdateFilePath(IElement serviceElement, RollingStockEntry vehicle)
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
