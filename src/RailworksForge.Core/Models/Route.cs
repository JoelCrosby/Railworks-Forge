namespace RailworksForge.Core.Models;

public record Route
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string RoutePropertiesPath { get; init; }

    public required string DirectoryPath { get; init; }

    public required PackagingType PackagingType { get; init; }

    public string MainContentArchivePath => Path.Join(DirectoryPath, "MainContent.ap");

    public virtual bool Equals(Route? other)
    {
        if (other is null) return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public void ExtractScenarios()
    {
        var archives = Directory.EnumerateFiles(DirectoryPath, "*.ap", SearchOption.TopDirectoryOnly);

        foreach (var archive in archives)
        {
            Archives.ExtractDirectory(archive, "Scenarios");
        }
    }
}
