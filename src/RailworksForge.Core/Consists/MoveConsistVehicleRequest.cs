namespace RailworksForge.Core.Consists;

public record MoveConsistVehicleRequest
{
    public required Consist Consist { get; init; }

    public required ConsistRailVehicle Vehicle { get; init; }

    public required int NewIndex { get; init; }
}
