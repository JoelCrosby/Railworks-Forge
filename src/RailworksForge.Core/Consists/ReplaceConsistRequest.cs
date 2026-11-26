namespace RailworksForge.Core.Consists;

public record ReplaceConsistRequest
{
    public required TargetConsist Target { get; init; }

    public required PreloadConsist PreloadConsist { get; init; }
}
