using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

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
        var document = context.ScenarioDocument;

        var serviceConsist = Consist.GetServiceConsist(document, _request.Consist);

        if (serviceConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        var railVehicle = serviceConsist
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .FirstOrDefault(el => el.GetAttribute("d:id") == _request.VehicleToDelete.Id);

        if (railVehicle is null)
        {
            throw new Exception("unable to find rail vehicle in scenario document");
        }

        var wasLeadVehicle = railVehicle.ParentElement?.FirstElementChild == railVehicle;
        var number = railVehicle.SelectTextContent("UniqueNumber");

        railVehicle.Remove();

        if (!string.IsNullOrEmpty(number))
        {
            ServiceVehicleUpdates.RemoveInitialVehicleNumber(serviceConsist, number);
        }

        if (!wasLeadVehicle)
        {
            return;
        }

        var entry = serviceConsist.QuerySelector("RailVehicles")?.FirstElementChild;

        if (entry is null)
        {
            return;
        }

        const string absoluteBlueprintId = "BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID";

        var blueprint = new Blueprint
        {
            BlueprintSetIdProvider = entry.SelectTextContent($"{absoluteBlueprintId} iBlueprintLibrary-cBlueprintSetID Provider"),
            BlueprintSetIdProduct = entry.SelectTextContent($"{absoluteBlueprintId} iBlueprintLibrary-cBlueprintSetID Product"),
            BlueprintId = entry.SelectTextContent($"{absoluteBlueprintId} BlueprintID"),
        };

        var vehicleDocument = await blueprint.GetXmlDocument();
        var vehicleElement = vehicleDocument.DocumentElement;
        var leadVehicle = RollingStockEntry.Parse(vehicleElement, blueprint);

        ServiceVehicleUpdates.UpdateLeadVehicle(context.ScenarioPropertiesDocument, _request.Consist, leadVehicle);
    }
}
