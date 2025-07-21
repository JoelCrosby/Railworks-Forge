using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public record TrackBlueprint(Blueprint Blueprint)
{
    public int Count { get; private set; } = 1;

    public void Add()
    {
        Count++;
    }
}
