using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public class Consist : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; set; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string ServiceId { get; init; }
}

public class ConsistRailVehicle : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? UniqueNumber { get; init; }

    public bool Flipped { get; init; }
}

public class ConsistBlueprint
{
    public required string LocomotiveName { get; init; }

    public required string DisplayName { get; init; }
}

public enum AcquisitionState
{
    Unknown = 0,
    Found = 1,
    Missing = 2,
}
