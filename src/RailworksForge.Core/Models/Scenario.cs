using System.Diagnostics;
using System.IO.Compression;

using AngleSharp.Dom;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Types;

namespace RailworksForge.Core.Models;

[DebuggerDisplay("{Name}")]
public record Scenario
{
    public required string Id { get; init; }

    public required Route Route { get; init; }

    public required string Name { get; init; }

    public string SearchIndex { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Briefing { get; init; }

    public string? StartLocation { get; init; }

    public required string Locomotive { get; init; }

    public required int Duration { get; init; }

    public required int Rating { get; init; }

    public string? Season { get; init; }

    public required string DirectoryPath { get; init; }

    public required string ScenarioPropertiesPath { get; init; }

    public required AssetPath AssetPath { get; init; }

    public List<Consist> Consists { get; init; } = [];

    public PackagingType PackagingType { get; init; }

    public ScenarioClass ScenarioClass { get; init; }

    public ScenarioPlayerInfo PlayerInfo { get; set; } = ScenarioPlayerInfo.Empty;

    public Consist? PlayerConsist => Consists.FirstOrDefault(consist => consist.PlayerDriver);

    public string CachedDocumentPath => Paths.GetAssetCachePath(BinaryPath, true);

    // A packed scenario only has its own folder once it has been edited or extracted.
    public string BrowsableDirectoryPath => Directory.Exists(DirectoryPath) ? DirectoryPath : Route.DirectoryPath;

    public string BackupDirectory => Path.Join(Paths.GetConfigurationFolder(), "backups", "scenarios", Id);

    private string BinaryPath => Path.Join(DirectoryPath, "Scenario.bin");
    private bool HasBinary => Paths.Exists(BinaryPath);

    // readArchivedDocument lets a caller that already has the archive open parse the entry without reopening it.
    public static Scenario? New(Route route, AssetPath assetPath, Func<IDocument>? readArchivedDocument = null)
    {
        var path = PreferLooseProperties(assetPath);
        var canUseArchivedReader = path.IsArchivePath && readArchivedDocument is not null;
        var doc = canUseArchivedReader ? readArchivedDocument!() : GetPropertiesDocument(path);

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (doc.DocumentElement?.FirstElementChild is null) return null;

        var id = doc.SelectTextContent("ID cGUID DevString").Trim();
        var name = doc.SelectLocalisedStringContent("DisplayName");
        var description = doc.SelectLocalisedStringContent("Description");
        var briefing = doc.SelectLocalisedStringContent("Briefing");
        var startLocation = doc.SelectLocalisedStringContent("StartLocation");
        var directoryPath = GetScenarioDirectory(path);
        var scenarioClass = doc.SelectTextContent("ScenarioClass");
        var season = ParseSeason(doc.SelectTextContent("Season"));
        var consists = doc.QuerySelectorAll("sDriverFrontEndDetails").Select(Consist.ParseConsist).ToList();
        var locomotive = consists.FirstOrDefault(c => c.PlayerDriver)?.LocomotiveName ?? string.Empty;
        var duration = doc.SelectInteger("DurationMins");
        var rating = doc.SelectInteger("Rating");

        return new Scenario
        {
            Id = id,
            Name = name,
            Description = description,
            Duration = duration,
            Briefing = briefing,
            StartLocation = startLocation,
            Locomotive = locomotive,
            DirectoryPath = directoryPath,
            AssetPath = path,
            ScenarioPropertiesPath = path.Path,
            Consists = consists,
            ScenarioClass = ScenarioClassTypes.Parse(scenarioClass),
            PackagingType = path.IsArchivePath ? PackagingType.Packed : PackagingType.Unpacked,
            Route = route,
            Rating = rating,
            Season = season,
            SearchIndex = name.ToLowerInvariant(),
        };
    }

    public Scenario? Refresh()
    {
        return New(Route, AssetPath);
    }

    public void CreateBackup()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            return;
        }

        Directory.CreateDirectory(BackupDirectory);

        var backupPath = Path.Join(BackupDirectory, Utilities.GetBackupArchiveName());

        ZipFile.CreateFromDirectory(DirectoryPath, backupPath);
    }

    // Edits to a packed scenario are written as loose files beside the archive, and the game prefers loose files.
    private static AssetPath PreferLooseProperties(AssetPath path)
    {
        if (!path.IsArchivePath)
        {
            return path;
        }

        var loosePath = Path.Join(GetScenarioDirectory(path), "ScenarioProperties.xml");
        var actualLoosePath = Paths.GetActualPathFromInsensitive(loosePath);

        if (actualLoosePath is null)
        {
            return path;
        }

        return new AssetPath { Path = actualLoosePath };
    }

    private static string GetScenarioDirectory(AssetPath path)
    {
        var containingDirectory = Path.GetDirectoryName(path.Path) ?? string.Empty;

        if (!path.IsArchivePath)
        {
            return containingDirectory;
        }

        var archiveDirectory = Path.GetDirectoryName(path.ArchivePath) ?? string.Empty;
        var scenarioDirectory = Path.Join(containingDirectory, archiveDirectory);

        // The archive's spelling may differ in case from a folder already on disk; reuse that folder rather than
        // creating a sibling.
        return Paths.GetActualPathFromInsensitive(scenarioDirectory, containingDirectory) ?? scenarioDirectory;
    }

    private static IDocument GetPropertiesDocument(AssetPath path)
    {
        if (Paths.Exists(path.Path) && path.Path.EndsWith(".xml"))
        {
            using var file = File.OpenRead(path.Path);
            return XmlParser.ParseDocument(file);
        }

        if (path.IsArchivePath)
        {
            return GetArchivedPropertiesDocument(path);
        }

        throw new Exception("failed to get properties document");
    }

    private static IDocument GetArchivedPropertiesDocument(AssetPath path)
    {
        using var archive = ZipFile.Open(path.Path, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault(e => e.FullName == path.ArchivePath);

        if (entry is null)
        {
            throw new Exception("could not file scenario properties entry in archive");
        }

        using var content = entry.Open();

        return XmlParser.ParseDocument(content);
    }

    public async Task<IDocument> GetXmlDocument(bool useCache = true)
    {
        var path = await ConvertBinToXml(useCache);
        await using var file = File.OpenRead(path);
        var document = await XmlParser.ParseDocumentAsync(file);

        XmlException.ThrowIfNotExists(document, path);

        return document;
    }

    public async Task<IDocument> GetPropertiesXmlDocument()
    {
        var text = GetPropertiesText() ?? GetCompressedPropertiesText();
        var document = await XmlParser.ParseDocumentAsync(text);

        if (document is null)
        {
            throw new Exception($"could not read RouteProperties.xml for scenario {Name}");
        }

        return document;
    }

    public async Task<string> ConvertBinToXml(bool useCache = true)
    {
        var inputPath = HasBinary ? BinaryPath : ExtractBinary();
        var result = await Serz.Convert(inputPath, force: !useCache);

        return result.OutputPath;
    }

    public async Task<string> ExportBinToXml()
    {
        var inputPath = HasBinary ? BinaryPath : ExtractBinary();
        var result = await Serz.Convert(inputPath, force: true);

        var filename = Path.GetFileName(result.OutputPath);
        var destination = Path.Join(DirectoryPath, filename);

        File.Copy(result.OutputPath, destination, overwrite: true);

        return destination;
    }

    private string ExtractBinary()
    {
        var archivePath = PackagingType is PackagingType.Packed ? AssetPath.Path : Route.MainContentArchivePath;

        if (!Paths.Exists(archivePath))
        {
            throw new FileNotFoundException($"could not find a Scenario.bin or archive for scenario {Name}");
        }

        var entryPath = $"Scenarios/{Id}/Scenario.bin";
        var extracted = Archives.ExtractFileContentFromPath(archivePath, entryPath, BinaryPath);

        if (!extracted)
        {
            throw new FileNotFoundException($"could not find {entryPath} in {archivePath}");
        }

        return BinaryPath;
    }

    private string? GetPropertiesText()
    {
        var idealPath = Path.Join(DirectoryPath, "ScenarioProperties.xml");
        var path = File.Exists(idealPath) ? idealPath : Paths.GetActualPathFromInsensitive(idealPath);

        if (path is null) return null;

        return File.ReadAllText(path);
    }

    private string GetCompressedPropertiesText()
    {
        if (AssetPath is { IsArchivePath: true, ArchivePath: { } archivePath })
        {
            return Archives.TryGetTextFileContentFromPath(AssetPath.Path, archivePath)
                ?? throw new Exception("could not find compressed scenario properties file");
        }

        var productArchives = Directory.EnumerateFiles(DirectoryPath, "*.ap", SearchOption.TopDirectoryOnly);

        foreach (var productArchive in productArchives)
        {
            var path = Path.Join("Scenarios", Id, "ScenarioProperties.xml");
            var result =  Archives.TryGetTextFileContentFromPath(productArchive, path);

            if (result is null) continue;

            return result;
        }

        throw new Exception("could not find compressed scenario properties file");

    }

    public async Task<string> ConvertXmlToBin()
    {
        CreateBackup();

        var path = Path.Join(DirectoryPath, "Scenario.bin.xml");
        var result = await Serz.Convert(path, force: true);

        File.Move(result.OutputPath, BinaryPath, overwrite: true);

        await Paths.CreateMd5HashFile(BinaryPath);

        return BinaryPath;
    }

    public async Task<List<ConsistRailVehicle>> GetServiceConsistVehicles(Consist consist)
    {
        var doc = await GetXmlDocument();

        if (!string.IsNullOrEmpty(consist.Id))
        {
            return doc
                .QuerySelectorAll("cConsist")
                .FirstOrDefault(el => el.GetAttribute("d:id") == consist.Id)?
                .QuerySelectorAll("RailVehicles cOwnedEntity")
                .Select(ParseConsist)
                .ToList() ?? [];
        }

        return doc
            .QuerySelectorAll("cConsist")
            .ElementAtOrDefault(consist.Index ?? 0)?
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .Select(ParseConsist)
            .ToList() ?? [];
    }

    private static ConsistRailVehicle ParseConsist(IElement el, int index)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContent("Name");
        var uniqueNumber = el.SelectTextContent("UniqueNumber");
        var blueprintId = el.SelectTextContent("BlueprintID BlueprintID");
        var flipped = el.SelectTextContent("Flipped") == "1";
        var blueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Provider");
        var entityID = el.SelectTextContent("EntityID cGUID DevString");

        return new ConsistRailVehicle
        {
            Id = consistId,
            Index = index,
            EntityID = entityID,
            LocomotiveName = locomotiveName,
            UniqueNumber = uniqueNumber,
            Flipped = flipped,
            BlueprintId = blueprintId,
            BlueprintSetIdProduct = blueprintSetIdProduct,
            BlueprintSetIdProvider = blueprintSetIdProvider,
            SearchIndex = $"{locomotiveName} {uniqueNumber} {blueprintSetIdProduct} {blueprintSetIdProvider} {blueprintId}".ToLowerInvariant(),
        };
    }

    private static string ParseSeason(string code)
    {
        return code switch
        {
            "SEASON_SPRING" => "Spring",
            "SEASON_SUMMER" => "Summer",
            "SEASON_AUTUMN" => "Autumn",
            "SEASON_WINTER" => "Winter",
            _ => string.Empty,
        };
    }

    public virtual bool Equals(Scenario? other)
    {
        return string.Equals(Id, other?.Id, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
    }

    public void SetPlayerInfo(ScenarioPlayerInfo playerInfo)
    {
        PlayerInfo = playerInfo;
    }
}
