using System.Diagnostics;
using System.IO.Compression;

using AngleSharp.Xml.Dom;

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

    public required string DirectoryPath { get; init; }

    public required string ScenarioPropertiesPath { get; init; }

    public required AssetPath AssetPath { get; set; }

    public List<Consist> Consists { get; init; } = [];

    public PackagingType PackagingType { get; init; }

    public ScenarioClass ScenarioClass { get; init; }

    private string BinaryPath => Path.Join(DirectoryPath, "Scenario.bin");
    private string BinaryXmlPath => Path.Join(DirectoryPath, "Scenario.bin.xml");

    private bool HasBinary => File.Exists(BinaryPath);
    private bool HasMainContentArchive => Paths.Exists(Route.MainContentArchivePath);

    public static Scenario New(Route route, AssetPath path)
    {
        var doc = GetPropertiesDocument(path);

        var id = doc.SelectTextContent("ID cGUID DevString");
        var name = doc.SelectTextContent("DisplayName English");
        var description = doc.SelectTextContent("description English");
        var briefing = doc.SelectTextContent("Briefing English");
        var startLocation = doc.SelectTextContent("StartLocation English");
        var directoryPath = Path.GetDirectoryName(path.Path) ?? string.Empty;
        var scenarioClass = doc.SelectTextContent("ScenarioClass");
        var consists = doc.QuerySelectorAll("sDriverFrontEndDetails").Select(Consist.Parse).ToList();
        var locomotive = consists.FirstOrDefault(c => c.PlayerDriver)?.LocomotiveName ?? string.Empty;

        return new Scenario
        {
            Id = id,
            Name = name,
            Description = description,
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
        };
    }

    public Scenario Refresh()
    {
        return New(Route, AssetPath);
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

    public async Task<IXmlDocument> GetXmlDocument()
    {
        var path = await ConvertBinToXml();
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

    public async Task<string> ConvertBinToXml()
    {
        if (File.Exists(BinaryXmlPath))
        {
            return BinaryXmlPath;
        }

        var inputPath = HasBinary ? BinaryPath : ExtractXml();
        var result = await Serz.Convert(inputPath);

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

        Archives.ExtractFileContentFromPath(Route.MainContentArchivePath, propertiesPath, destination);

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
}
