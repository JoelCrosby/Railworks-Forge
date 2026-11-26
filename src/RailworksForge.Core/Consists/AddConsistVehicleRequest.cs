using RailworksForge.Core.Vehicles;

namespace RailworksForge.Core.Consists;

public class AddConsistVehicleRequest
{
    public required Consist Consist { get; init; }

    public required RollingStockEntry VehicleToAdd { get; init; }
}
