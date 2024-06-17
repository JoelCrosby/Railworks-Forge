namespace RailworksForge.Core.Models;

public record SavedConsist
{
    public required string Name { get; init; }

    public required Consist Consist { get; init; }
}
