namespace RailworksForge.Core.Consists;

public record DeleteConsistVehicleRequest
{
    public required  ConsistRailVehicle VehicleToDelete { get; init; }

    public required  Consist Consist { get; init; }
}
