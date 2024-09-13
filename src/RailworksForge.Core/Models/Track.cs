using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public record Track
{
    public required string Name { get; init; }

    public required Blueprint Blueprint { get; init; }

    public string DisplayName => string.IsNullOrEmpty(Name) is false ? Name : Blueprint.BlueprintId;
}
