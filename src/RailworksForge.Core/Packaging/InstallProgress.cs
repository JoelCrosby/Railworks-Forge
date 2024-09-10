namespace RailworksForge.Core.Packaging;

public record InstallProgress
{
    public required int Progress { get; init; }

    public required  string CurrentTask { get; init; }

    public required  string Message { get; init; }

    public required bool IsLoading { get; init; }
}
