namespace RailworksForge.Core.Models;

public record Route
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Path { get; init; }

    public required string RootPath { get; init; }

    public required PackagingType PackagingType { get; init; }
}
