namespace RailworksForge.Core.Packaging;

public record Package
{
    public required string Name { get; init; }

    public required string Author { get; init; }

    public required Protection Protection { get; init; }

    public required Dictionary<string, string?> Assets { get; init; }
}
