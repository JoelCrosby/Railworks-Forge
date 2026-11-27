using AngleSharp.Dom;

using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Consists.Commands;

// A consist's vehicles run front to back in document order, and the driver's InitialRV lists their numbers in the
// same order, so both are reordered together.
internal static class ConsistVehicleOrder
{
    public static List<IElement> GetVehicles(IElement railVehicles)
    {
        return railVehicles.Children.Where(element => element.LocalName == "cOwnedEntity").ToList();
    }

    public static async Task PlaceAt(ConsistCommandContext context, Consist consist, IElement serviceConsist, IElement vehicle, int position)
    {
        var vehicles = GetVehicles(vehicle.ParentElement!);
        var currentIndex = vehicles.IndexOf(vehicle);
        var newIndex = Math.Clamp(position, 0, vehicles.Count - 1);

        if (newIndex == currentIndex)
        {
            return;
        }

        vehicles.RemoveAt(currentIndex);
        vehicles.Insert(newIndex, vehicle);

        await Apply(context, consist, serviceConsist, vehicles);
    }

    public static async Task Apply(ConsistCommandContext context, Consist consist, IElement serviceConsist, List<IElement> ordered)
    {
        var railVehicles = ordered[0].ParentElement!;
        var previousLead = railVehicles.FirstElementChild;

        foreach (var vehicle in ordered)
        {
            railVehicles.AppendChild(vehicle);
        }

        ReorderInitialVehicleNumbers(serviceConsist, ordered);

        var leadChanged = ordered[0] != previousLead;

        if (leadChanged)
        {
            await ServiceVehicleUpdates.UpdateLeadVehicleFrom(context.ScenarioPropertiesDocument, consist, ordered[0]);
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
