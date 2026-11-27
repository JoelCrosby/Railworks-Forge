using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Consists.Commands;

public class DeleteConsistVehicles : IConsistCommand
{
    private readonly DeleteConsistVehiclesRequest _request;

    public DeleteConsistVehicles(DeleteConsistVehiclesRequest request)
    {
        _request = request;
    }

    public async Task Run(ConsistCommandContext context)
    {
        var serviceConsist = ConsistVehicleElements.FindServiceConsist(context.ScenarioDocument, _request.Consist);
        var railVehicles = serviceConsist.QuerySelector("RailVehicles");
        var previousLead = railVehicles?.FirstElementChild;
        var vehicleCount = railVehicles is null ? 0 : ConsistVehicleOrder.GetVehicles(railVehicles).Count;
        var removesEveryVehicle = _request.Vehicles.Count >= vehicleCount;

        // A consist with no vehicles can't be saved or added to, since new vehicles are copied from an existing one.
        if (removesEveryVehicle)
        {
            throw new InvalidOperationException("A consist must keep at least one vehicle.");
        }

        foreach (var vehicle in _request.Vehicles)
        {
            var railVehicle = ConsistVehicleElements.FindVehicle(serviceConsist, vehicle);
            var number = railVehicle.SelectTextContent("UniqueNumber");

            railVehicle.Remove();

            if (!string.IsNullOrEmpty(number))
            {
                ServiceVehicleUpdates.RemoveInitialVehicleNumber(serviceConsist, number);
            }
        }

        var lead = railVehicles?.FirstElementChild;
        var leadChanged = lead is not null && lead != previousLead;

        if (leadChanged)
        {
            await ServiceVehicleUpdates.UpdateLeadVehicleFrom(context.ScenarioPropertiesDocument, _request.Consist, lead!);
        }
    }
}
