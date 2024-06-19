namespace RailworksForge.Core.Models;

public record SavedConsist
{
    public required string Name { get; init; }

    public required string LocomotiveName { get; init; }

    public required string ConsistElement { get; init; }
}
