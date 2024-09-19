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

    private string BinaryPath => Path.Join(DirectoryPath, "Scenario.bin");

    private bool HasBinary => File.Exists(BinaryPath);
    private bool HasMainContentArchive => Paths.Exists(Route.MainContentArchivePath);

    public string CachedDocumentPath => Paths.GetAssetCachePath(BinaryPath, true);

    public string BackupDirectory => Path.Join(Paths.GetConfigurationFolder(), "backups", "scenarios", Id);

    public static Scenario? New(Route route, AssetPath path)
    {
        var doc = GetPropertiesDocument(path);

        if (doc.DocumentElement.FirstElementChild is null) return null;

        var id = doc.SelectTextContent("ID cGUID DevString").Trim();
        var name = doc.SelectLocalisedStringContent("DisplayName");
        var description = doc.SelectLocalisedStringContent("Description");
        var briefing = doc.SelectLocalisedStringContent("Briefing");
        var startLocation = doc.SelectLocalisedStringContent("StartLocation");
        var directoryPath = Path.GetDirectoryName(path.Path) ?? string.Empty;
        var scenarioClass = doc.SelectTextContent("ScenarioClass");
        var season = ParseSeason(doc.SelectTextContent("Season"));
        var consists = doc.QuerySelectorAll("sDriverFrontEndDetails").Select(Consist.ParseConsist).ToList();
        var locomotive = consists.FirstOrDefault(c => c.PlayerDriver)?.LocomotiveName ?? string.Empty;

        _ = int.TryParse(doc.SelectTextContent("DurationMins"), out var duration);
        _ = int.TryParse(doc.SelectTextContent("Rating"), out var rating);

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
        };
    }

    public Scenario? Refresh()
    {
        return New(Route, AssetPath);
    }

    public void CreateBackup()
    {
        Directory.CreateDirectory(BackupDirectory);

        var backupPath = Path.Join(BackupDirectory, Utilities.GetBackupArchiveName());

        ZipFile.CreateFromDirectory(DirectoryPath, backupPath);
    }

    private static IDocument GetPropertiesDocument(AssetPath path)
    {
        if (Paths.Exists(path.Path) && path.Path.EndsWith(".xml"))
        {
            var content = File.ReadAllText(path.Path);
            return XmlParser.ParseDocument(content);
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

        var content = entry.Open();

        using var reader = new StreamReader(content);

        var file = reader.ReadToEnd();
        return XmlParser.ParseDocument(file);
    }

    public async Task<IDocument> GetXmlDocument(bool useCache = true)
    {
        var path = await ConvertBinToXml(useCache);
        var text = await File.ReadAllTextAsync(path);
        var document = await XmlParser.ParseDocumentAsync(text);

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
        var inputPath = HasBinary ? BinaryPath : ExtractXml();
        var result = await Serz.Convert(inputPath, force: !useCache);

        return result.OutputPath;
    }

    public async Task<string> ExportBinToXml()
    {
        var inputPath = HasBinary ? BinaryPath : ExtractXml();
        var result = await Serz.Convert(inputPath, force: true);

        var filename = Path.GetFileName(result.OutputPath);
        var destination = Path.Join(DirectoryPath, filename);

        File.Copy(result.OutputPath, destination);

        return destination;
    }

    private string ExtractXml()
    {
        if (!HasMainContentArchive)
        {
            throw new NotImplementedException("scenario does not contain a MainContent.ap");
        }

        var propertiesPath = Path.Join("Scenarios", Id, "Scenario.bin");
        var destination = Path.Join(Route.DirectoryPath, "Scenarios", Id, "Scenario.bin");
        var archivePath = PackagingType is PackagingType.Packed ? AssetPath.Path : Route.MainContentArchivePath;

        Archives.ExtractFileContentFromPath(archivePath, propertiesPath, destination);

        return destination;
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
        var path = Path.Join(DirectoryPath, "Scenario.bin.xml");
        var result = await Serz.Convert(path, force: true);

        var filename = Path.GetFileName(result.OutputPath);
        var destination = Path.Join(DirectoryPath, filename);

        File.Copy(result.OutputPath, destination);

        return BinaryPath;
    }

    public async Task<List<ConsistRailVehicle>> GetServiceConsistVehicles(string consistId)
    {
        var doc = await GetXmlDocument(false);

        return doc
            .QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.GetAttribute("d:id") == consistId)?
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .Select(ParseConsist)
            .ToList() ?? [];
    }

    private static ConsistRailVehicle ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContent("Name");
        var uniqueNumber = el.SelectTextContent("UniqueNumber");
        var blueprintId = el.SelectTextContent("BlueprintID BlueprintID");
        var flipped = el.SelectTextContent("Flipped") == "1";
        var blueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Provider");

        return new ConsistRailVehicle
        {
            Id = consistId,
            LocomotiveName = locomotiveName,
            UniqueNumber = uniqueNumber,
            Flipped = flipped,
            BlueprintId = blueprintId,
            BlueprintSetIdProduct = blueprintSetIdProduct,
            BlueprintSetIdProvider = blueprintSetIdProvider,
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
        return Id.GetHashCode();
    }
}
