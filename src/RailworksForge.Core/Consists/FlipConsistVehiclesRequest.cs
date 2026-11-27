namespace RailworksForge.Core.Consists;

public record FlipConsistVehiclesRequest
{
    public required Consist Consist { get; init; }

    public required IReadOnlyList<ConsistRailVehicle> Vehicles { get; init; }
}
