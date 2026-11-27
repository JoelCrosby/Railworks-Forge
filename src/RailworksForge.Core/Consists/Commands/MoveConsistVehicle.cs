using AngleSharp.Dom;

using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Consists.Commands;

// A consist's vehicles run front to back in document order, and the driver's InitialRV lists their numbers in the
// same order, so both are reordered together.
public class MoveConsistVehicle : IConsistCommand
{
    private readonly MoveConsistVehicleRequest _request;

    public MoveConsistVehicle(MoveConsistVehicleRequest request)
    {
        _request = request;
    }

    public async Task Run(ConsistCommandContext context)
    {
        var serviceConsist = ConsistVehicleElements.FindServiceConsist(context.ScenarioDocument, _request.Consist);
        var railVehicle = ConsistVehicleElements.FindVehicle(serviceConsist, _request.Vehicle);
        var container = railVehicle.ParentElement!;
        var vehicles = container.Children.Where(element => element.LocalName == "cOwnedEntity").ToList();
        var currentIndex = vehicles.IndexOf(railVehicle);
        var newIndex = Math.Clamp(_request.NewIndex, 0, vehicles.Count - 1);

        if (newIndex == currentIndex)
        {
            return;
        }

        var previousLead = vehicles[0];

        vehicles.RemoveAt(currentIndex);
        vehicles.Insert(newIndex, railVehicle);

        foreach (var vehicle in vehicles)
        {
            container.AppendChild(vehicle);
        }

        ReorderInitialVehicleNumbers(serviceConsist, vehicles);

        var leadChanged = vehicles[0] != previousLead;

        if (leadChanged)
        {
            await ServiceVehicleUpdates.UpdateLeadVehicleFrom(context.ScenarioPropertiesDocument, _request.Consist, vehicles[0]);
        }
    }

    private static void ReorderInitialVehicleNumbers(IElement serviceConsist, List<IElement> vehicles)
    {
        if (serviceConsist.QuerySelector("Driver cDriver InitialRV") is not { } initialRv)
        {
            return;
        }

        var entries = initialRv.Children.ToList();
        var vehicleNumbers = vehicles.Select(vehicle => vehicle.SelectTextContent("UniqueNumber")).ToList();
        var listsEveryVehicle = entries.Count == vehicleNumbers.Count;

        if (!listsEveryVehicle)
        {
            return;
        }

        var remaining = new List<IElement>(entries);
        var ordered = new List<IElement>();

        foreach (var number in vehicleNumbers)
        {
            var entry = remaining.FirstOrDefault(candidate => candidate.TextContent == number);

            if (entry is null)
            {
                return;
            }

            remaining.Remove(entry);
            ordered.Add(entry);
        }

        foreach (var entry in ordered)
        {
            initialRv.AppendChild(entry);
        }
    }
}
