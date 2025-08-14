using System.Globalization;

namespace RailworksForge.Core.Config;

public record ConfigurationOptions
{
    public string GameDirectoryPath { get; set; }

    public Dictionary<string, DataGridOptions> DataGrids { get; set; } = new();

    public string Language { get; set; }

    public string Theme { get; set; }

    public ConfigurationOptions()
    {
        GameDirectoryPath = string.Empty;
        DataGrids = new();
        Language = CultureInfo.CurrentCulture.Name;
        Theme = "System";
    }
}

public record DataGridOptions
{
    public string? SortingColumn { get; set; }

    public string? SortingDirection { get; set; }
}
