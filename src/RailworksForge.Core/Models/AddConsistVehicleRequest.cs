namespace RailworksForge.Core.Models;

public class AddConsistVehicleRequest
{
    public required Consist Consist { get; init; }

    public required RollingStockEntry VehicleToAdd { get; init; }
}
