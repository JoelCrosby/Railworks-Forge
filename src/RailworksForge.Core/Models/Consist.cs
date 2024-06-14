namespace RailworksForge.Core.Models;

public class Consist
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; set; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string BlueprintId { get; init; }

    public string? RawText { get; init; }
}

public class ConsistRailVehicle
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? UniqueNumber { get; init; }

    public bool Flipped { get; init; }
}
