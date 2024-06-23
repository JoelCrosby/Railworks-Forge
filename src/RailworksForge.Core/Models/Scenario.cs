using AngleSharp.Dom;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;

namespace RailworksForge.Core.Models;

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

    public List<Consist> Consists { get; init; } = [];

    public required PackagingType PackagingType { get; init; }

    public required ScenarioClass ScenarioClass { get; init; }

    private string BinaryPath => Path.Join(DirectoryPath, "Scenario.bin");
    private string BinaryXmlPath => Path.Join(DirectoryPath, "Scenario.bin.xml");

    private bool HasBinary => File.Exists(BinaryPath);
    private bool HasMainContentArchive => Paths.Exists(Route.MainContentArchivePath);

    public static Scenario Parse(IElement el, Route route, string path)
    {
        var id = el.SelectTextContent("ID cGUID DevString");
        var name = el.SelectTextContent("DisplayName English");
        var description = el.SelectTextContent("description English");
        var briefing = el.SelectTextContent("Briefing English");
        var startLocation = el.SelectTextContent("StartLocation English");
        var directoryPath = Path.GetDirectoryName(path) ?? string.Empty;
        var scenarioClass = el.SelectTextContent("ScenarioClass");

        var consists = el.QuerySelectorAll("sDriverFrontEndDetails").Select(Consist.Parse).ToList();

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
            ScenarioPropertiesPath = path,
            Consists = consists,
            ScenarioClass = ScenarioClassTypes.Parse(scenarioClass),
            PackagingType = path.EndsWith(".xml") ? PackagingType.Unpacked : PackagingType.Packed,
            Route = route,
        };
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
        var outputPath = inputPath.Replace(".bin", ".bin.xml");

        await Serz.Convert(inputPath);

        return outputPath;
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

    public virtual bool Equals(Scenario? other)
    {
        if (other is null) return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
