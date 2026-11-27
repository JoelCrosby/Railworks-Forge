namespace RailworksForge.Core.Consists;

public record FlipConsistVehicleRequest
{
    public required Consist Consist { get; init; }

    public required ConsistRailVehicle Vehicle { get; init; }
}
