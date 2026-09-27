namespace RailworksForge.Core.Packaging;

public record InstallProgress
{
    public required InstallStage Stage { get; init; }

    public required string PackageName { get; init; }

    public required int Progress { get; init; }

    public int FilesProcessed { get; init; }

    public int FileCount { get; init; }

    public required bool IsLoading { get; init; }
}
