namespace RailworksForge.Core.Config;

public record ConfigurationOptions
{
    public required string GameDirectoryPath { get; set; }

    public Dictionary<string, DataGridOptions> DataGrids { get; set; } = new();
}

public record DataGridOptions
{
    public string? SortingColumn { get; set; }

    public string? SortingDirection { get; set; }
}
