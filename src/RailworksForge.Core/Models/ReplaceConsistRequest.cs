using RailworksForge.Core.Models;

namespace RailworksForge.Core.models;

public record ReplaceConsistRequest
{
    public required TargetConsist Target { get; init; }

    public required PreloadConsist PreloadConsist { get; init; }
}
