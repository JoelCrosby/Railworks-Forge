using RailworksForge.Core.Vehicles;

namespace RailworksForge.Core.Consists;

public class AddConsistVehicleRequest
{
    public required Consist Consist { get; init; }

    public required RollingStockEntry VehicleToAdd { get; init; }

    // Index the new vehicle takes, counted from the front; null adds it at the rear.
    public int? Position { get; init; }
}
