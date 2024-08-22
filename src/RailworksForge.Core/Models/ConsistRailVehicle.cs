using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public record ConsistRailVehicle : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? UniqueNumber { get; init; }

    public bool Flipped { get; init; }
}
