using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands;

public class AddConsistVehicle : IConsistCommand
{
    private readonly AddConsistVehicleRequest _request;

    public AddConsistVehicle(AddConsistVehicleRequest request)
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

        var railVehicles = serviceConsist.QuerySelector("RailVehicles");
        var lastVehicle = railVehicles?.LastElementChild;

        if (railVehicles is null || lastVehicle is null)
        {
            throw new Exception("unable to find rail vehicles in scenario consist");
        }

        var blueprint = _request.VehicleToAdd.Blueprint;
        var vehicleDocument = await blueprint.GetXmlDocument();
        var scenarioConsist = ScenarioConsist.ParseConsist(vehicleDocument, blueprint);
        var generated = await VehicleGenerator.GenerateVehicle(document, lastVehicle, scenarioConsist, blueprint, false);

        railVehicles.AppendChild(generated.Element);

        ServiceVehicleUpdates.AppendInitialVehicleNumber(document, serviceConsist, generated.Number);
        ServiceVehicleUpdates.AddRequiredBlueprintSet(context.ScenarioPropertiesDocument, blueprint);
    }
}
