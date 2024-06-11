namespace RailworksForge.Core.Models;

public class Consist
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public string? RawText { get; init; }
}

public class ConsistRailVehicle
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }
}
