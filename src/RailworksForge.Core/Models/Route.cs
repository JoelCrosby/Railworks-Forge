using System.Diagnostics;
using System.IO.Compression;

using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

[DebuggerDisplay("{Name}")]
public record Route
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string RoutePropertiesPath { get; init; }

    public required string DirectoryPath { get; init; }

    public required PackagingType PackagingType { get; init; }

    public string MainContentArchivePath => Path.Join(DirectoryPath, "MainContent.ap");

    public string TracksBinaryPath => Path.Join(DirectoryPath, "Networks", "Tracks.bin");

    public string BackupDirectory => Path.Join(Paths.GetConfigurationFolder(), "backups", "routes", Id);

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

    // Only the files a track replacement rewrites; anything not loose is still intact inside the route archives.
    public void CreateBackup()
    {
        var routePropertiesPath = Path.Join(DirectoryPath, "RouteProperties.xml");
        var loosePaths = new[] { TracksBinaryPath, routePropertiesPath }.Where(File.Exists).ToList();

        if (loosePaths.Count is 0)
        {
            return;
        }

        Directory.CreateDirectory(BackupDirectory);

        var backupPath = Path.Join(BackupDirectory, Utilities.GetBackupArchiveName());

        using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);

        foreach (var path in loosePaths)
        {
            var entryName = Path.GetRelativePath(DirectoryPath, path).Replace('\\', '/');

            archive.CreateEntryFromFile(path, entryName);
        }
    }

    public async Task<List<TrackBlueprint>> GetTrackBlueprints()
    {
        var document = await GetTrackDocument();

        if (document is null)
        {
            throw new Exception("could not read route tracks file");
        }

        var blueprints = document
                .QuerySelectorAll("Network-cSectionGenericProperties BlueprintID")
                .Aggregate(new List<TrackBlueprint>(), (results, element) =>
                {
                    var blueprintId = element.SelectTextContent("BlueprintID");

                    if (string.IsNullOrEmpty(blueprintId))
                    {
                        return results;
                    }

                    if (results.FirstOrDefault(r => r.Blueprint.BlueprintId == blueprintId) is {} result)
                    {
                        result.Add();
                    }
                    else
                    {
                        var provider = element.SelectTextContent("Provider");
                        var product = element.SelectTextContent("Product");

                        var blueprint = new Blueprint
                        {
                            BlueprintId = blueprintId,
                            BlueprintSetIdProduct = product,
                            BlueprintSetIdProvider = provider,
                        };

                        results.Add(new TrackBlueprint(blueprint));
                    }

                    return results;
                });

        return blueprints.ToList();
    }

    public async Task<IDocument?> GetTrackDocument()
    {
        var path = TracksBinaryPath;

        if (Paths.Exists(path))
        {
            var output = await Serz.Convert(path, force: true);

            return await ParseXmlFile(output.OutputPath);
        }

        var archivePath = MainContentArchivePath;
        var destination = Paths.GetAssetCachePath(path, false);

        Archives.ExtractFileContentFromPath(archivePath, "Networks/Tracks.bin", destination);

        var compressedOutput = await Serz.Convert(destination);

        if (Paths.Exists(compressedOutput.OutputPath))
        {
            return await ParseXmlFile(compressedOutput.OutputPath);
        }

        return null;
    }

    private static async Task<IDocument> ParseXmlFile(string path)
    {
        await using var file = File.OpenRead(path);

        return await XmlParser.ParseDocumentAsync(file);
    }

    public async Task<IDocument?> GetRoutePropertiesDocument()
    {
        // Track replacement writes a loose RouteProperties.xml beside a packed route's archive, and the game prefers it.
        var loosePropertiesPath = Paths.GetActualPathFromInsensitive(Path.Join(DirectoryPath, "RouteProperties.xml"));

        if (loosePropertiesPath is not null)
        {
            return await ParseXmlFile(loosePropertiesPath);
        }

        if (PackagingType is PackagingType.Packed)
        {
            return await GetArchivedPropertiesDocument();
        }

        return await ParseXmlFile(RoutePropertiesPath);
    }

    private Task<IDocument> GetArchivedPropertiesDocument()
    {
        var archivePath = Path.Join(DirectoryPath, "MainContent.ap");

        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault(e => e.FullName == "RouteProperties.xml");

        if (entry is null)
        {
            throw new Exception("could not file scenario properties entry in archive");
        }

        using var content = entry.Open();
        using var reader = new StreamReader(content);

        var file = reader.ReadToEnd();
        return XmlParser.ParseDocumentAsync(file);
    }
}
