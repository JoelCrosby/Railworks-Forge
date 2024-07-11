namespace RailworksForge.Core.Types;

public record AssetPath
{
    public required string Path { get; init; }

    public bool IsArchivePath { get; init; }

    public string? ArchivePath { get; init; }

    public static readonly AssetPath Empty = new () { Path = string.Empty };
}
