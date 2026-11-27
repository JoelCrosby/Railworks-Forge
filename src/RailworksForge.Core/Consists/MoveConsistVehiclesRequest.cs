namespace RailworksForge.Core.Consists;

public record MoveConsistVehiclesRequest
{
    public required Consist Consist { get; init; }

    public required IReadOnlyList<ConsistRailVehicle> Vehicles { get; init; }

    // Places to move toward the rear; negative moves toward the front.
    public required int Offset { get; init; }
}
