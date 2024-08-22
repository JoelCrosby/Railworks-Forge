using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public record ReplaceTracksRequest
{
    public required List<TrackReplacement> Replacements { get; init; }
}

public record TrackReplacement
{
    public required Blueprint Blueprint { get; init; }

    public required Blueprint? ReplacementBlueprint { get; init; }
}
