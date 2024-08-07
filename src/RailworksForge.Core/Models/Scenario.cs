using System.Diagnostics;
using System.IO.Compression;

using AngleSharp.Dom;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models.Common;
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

    public required AssetPath AssetPath { get; set; }

    public List<Consist> Consists { get; init; } = [];

    public PackagingType PackagingType { get; init; }

    public ScenarioClass ScenarioClass { get; init; }

    private string BinaryPath => Path.Join(DirectoryPath, "Scenario.bin");

    private bool HasBinary => File.Exists(BinaryPath);
    private bool HasMainContentArchive => Paths.Exists(Route.MainContentArchivePath);

    public string CachedDocumentPath => Paths.GetAssetCachePath(BinaryPath, true);

    public static Scenario New(Route route, AssetPath path)
    {
        var doc = GetPropertiesDocument(path);

        var id = doc.SelectTextContent("ID cGUID DevString").Trim();
        var name = doc.SelectTextContent("DisplayName English");
        var description = doc.SelectTextContent("description English");
        var briefing = doc.SelectTextContent("Briefing English");
        var startLocation = doc.SelectTextContent("StartLocation English");
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

    public Scenario Refresh()
    {
        return New(Route, AssetPath);
    }

    private string GetBackupDirectory()
    {
        return Path.Join(Paths.GetConfigurationFolder(), "backups", "scenarios", Id);
    }

    public void CreateBackup()
    {
        var backupOutputDirectory = GetBackupDirectory();

        Directory.CreateDirectory(backupOutputDirectory);

        var backupPath = Path.Join(backupOutputDirectory, $"backup-{Guid.NewGuid().ToString()[..6]}-{DateTimeOffset.UtcNow:dd-MMM-yy_hh-mm}.zip");

        ZipFile.CreateFromDirectory(DirectoryPath, backupPath);
    }

    private static IXmlDocument GetPropertiesDocument(AssetPath path)
    {
        if (path.IsArchivePath)
        {
            return GetArchivedPropertiesDocument(path);
        }

        var content = File.ReadAllText(path.Path);

        return XmlParser.ParseDocument(content);
    }

    private static IXmlDocument GetArchivedPropertiesDocument(AssetPath path)
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

    public async Task<IXmlDocument> GetXmlDocument(bool useCache = true)
    {
        var path = await ConvertBinToXml(useCache);
        var text = await File.ReadAllTextAsync(path);
        var document = await XmlParser.ParseDocumentAsync(text);

        XmlException.ThrowIfNotExists(document, path);

        return document;
    }

    public async Task<IXmlDocument> GetPropertiesXmlDocument()
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
        var result = await Serz.Convert(inputPath, !useCache);

        return result.OutputPath;
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
        if (!HasMainContentArchive)
        {
            throw new NotImplementedException("scenario does not contain a MainContent.ap");
        }

        return Archives.GetTextFileContentFromPath(Route.MainContentArchivePath, "ScenarioProperties.xml");
    }

    public async Task<string> ConvertXmlToBin()
    {
        var path = Path.Join(DirectoryPath, "Scenario.bin.xml");

        await Serz.Convert(path);

        return BinaryPath;
    }

    public async Task<List<Blueprint>> GetBlueprintIds()
    {
        var doc = await GetXmlDocument();

        return doc
            .QuerySelectorAll("cConsist RailVehicles cOwnedEntity BlueprintID")
            .Select(Blueprint.Parse)
            .ToList();
    }

    public async Task<List<ConsistRailVehicle>> GetServiceConsistVehicles(string serviceId)
    {
        var doc = await GetXmlDocument();

        return doc
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("ServiceName Key", serviceId)?
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

    public async Task GetConsistStatus()
    {
        foreach (var consist in Consists)
        {
            var consists = await GetServiceConsistVehicles(consist.ServiceId);
            var state = consists.All(c => c.AcquisitionState == AcquisitionState.Found)
                ? AcquisitionState.Found
                : AcquisitionState.Missing;

            consist.ConsistAcquisitionState = state;
        }
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
