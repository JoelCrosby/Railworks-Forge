using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public record ReplaceTracksRequest
{
    public required List<TrackReplacement> Replacements { get; init; }

    public IEnumerable<TrackReplacement> GetSelectedReplacements()
    {
        return Replacements.Where(r => r.ReplacementBlueprint is not null);
    }
}

public record TrackReplacement
{
    public required Blueprint Blueprint { get; init; }

    public required Blueprint? ReplacementBlueprint { get; init; }
}
