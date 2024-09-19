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

    public void CreateBackup()
    {
        Directory.CreateDirectory(BackupDirectory);

        var backupPath = Path.Join(BackupDirectory, Utilities.GetBackupArchiveName());

        ZipFile.CreateFromDirectory(DirectoryPath, backupPath);
    }

    public async Task<List<Blueprint>> GetTrackBlueprints()
    {
        var document = await GetTrackDocument();

        if (document is null)
        {
            throw new Exception("could not read route tracks file");
        }

        var blueprints = document
                .QuerySelectorAll("Network-cSectionGenericProperties BlueprintID")
                .Select(element =>
                {
                    var provider = element.SelectTextContent("Provider");
                    var product = element.SelectTextContent("Product");
                    var blueprintId = element.SelectTextContent("BlueprintID");

                    return new Blueprint
                    {
                        BlueprintId = blueprintId,
                        BlueprintSetIdProduct = product,
                        BlueprintSetIdProvider = provider,
                    };
                });

        return blueprints.ToList();
    }

    public async Task<IDocument?> GetTrackDocument()
    {
        var path = TracksBinaryPath;

        if (Paths.Exists(path))
        {
            var output = await Serz.Convert(path, force: true);
            var xml = await File.ReadAllTextAsync(output.OutputPath);

            return await XmlParser.ParseDocumentAsync(xml);
        }

        var archivePath = MainContentArchivePath;
        var destination = Paths.GetAssetCachePath(path, false);

        Archives.ExtractFileContentFromPath(archivePath, "Networks/Tracks.bin", destination);

        var compressedOutput = await Serz.Convert(destination);

        if (Paths.Exists(compressedOutput.OutputPath))
        {
            var xml = await File.ReadAllTextAsync(compressedOutput.OutputPath);
            return await XmlParser.ParseDocumentAsync(xml);
        }

        return null;
    }

    public async Task<IDocument?> GetRoutePropertiesDocument()
    {
        if (PackagingType is PackagingType.Packed)
        {
            return await GetArchivedPropertiesDocument();
        }

        var text = await File.ReadAllTextAsync(RoutePropertiesPath);
        return await XmlParser.ParseDocumentAsync(text);
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

        var content = entry.Open();

        using var reader = new StreamReader(content);

        var file = reader.ReadToEnd();
        return XmlParser.ParseDocumentAsync(file);
    }
}
