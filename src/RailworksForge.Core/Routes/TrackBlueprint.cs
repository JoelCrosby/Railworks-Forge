using RailworksForge.Core.Assets;

namespace RailworksForge.Core.Routes;

public record TrackBlueprint(Blueprint Blueprint)
{
    public int Count { get; private set; } = 1;

    public void Add()
    {
        Count++;
    }
}
