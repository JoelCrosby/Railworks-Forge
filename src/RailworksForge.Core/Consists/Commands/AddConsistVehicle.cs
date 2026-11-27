using RailworksForge.Core.Vehicles;

namespace RailworksForge.Core.Consists.Commands;

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
        var existing = railVehicles is null ? [] : ConsistVehicleOrder.GetVehicles(railVehicles);

        if (railVehicles is null || existing.Count is 0)
        {
            throw new Exception("unable to find rail vehicles in scenario consist");
        }

        var rearPosition = existing.Count;
        var position = Math.Clamp(_request.Position ?? rearPosition, 0, rearPosition);

        // The new vehicle copies its track followers and position from the vehicle it couples behind.
        var templateIndex = Math.Max(position - 1, 0);
        var template = existing[templateIndex];

        var blueprint = _request.VehicleToAdd.Blueprint;
        var vehicleDocument = await blueprint.GetXmlDocument();
        var scenarioConsist = ScenarioConsist.ParseConsist(vehicleDocument, blueprint);
        var generated = await VehicleGenerator.GenerateVehicle(document, template, scenarioConsist, blueprint, false);

        railVehicles.AppendChild(generated.Element);

        ServiceVehicleUpdates.AppendInitialVehicleNumber(document, serviceConsist, generated.Number);
        ServiceVehicleUpdates.AddRequiredBlueprintSet(context.ScenarioPropertiesDocument, blueprint);

        await ConsistVehicleOrder.PlaceAt(context, _request.Consist, serviceConsist, generated.Element, position);
    }
}
