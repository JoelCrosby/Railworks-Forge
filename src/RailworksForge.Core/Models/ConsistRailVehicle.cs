using System.Runtime.CompilerServices;

using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public class ConsistRailVehicle : Blueprint
{
    public required string Id { get; init; }

    public int Index { get; init; }

    public required string LocomotiveName { get; init; }

    public string? UniqueNumber { get; init; }

    public bool Flipped { get; init; }

    public required string EntityID { get; init; }

    public required string SearchIndex { get; init; }

    public override bool Equals(object? other) => ReferenceEquals(this, other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
