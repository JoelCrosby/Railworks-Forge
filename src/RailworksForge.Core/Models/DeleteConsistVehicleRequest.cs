namespace RailworksForge.Core.Models;

public record DeleteConsistVehicleRequest
{
    public required  ConsistRailVehicle VehicleToDelete { get; init; }

    public required  Consist Consist { get; init; }
}
