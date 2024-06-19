using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

using RailworksForge.Core.Exceptions;
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

    public string? Locomotive { get; init; }

    public string? DirectoryPath { get; init; }

    public string? ScenarioPropertiesPath { get; init; }

    public List<Consist> Consists { get; init; } = [];

    public required PackagingType PackagingType { get; init; }

    public required ScenarioClass ScenarioClass { get; init; }

    private string ScenarioBinaryPath => Path.Join(DirectoryPath, "Scenario.bin");
    private string ScenarioBinaryXmlPath => Path.Join(DirectoryPath, "Scenario.bin.xml");

    private bool HasScenarioBinary => File.Exists(ScenarioBinaryPath);
    private bool HasMainContentArchive => File.Exists(Route.MainContentArchivePath);

    public async Task<string> ConvertBinToXml()
    {
        if (File.Exists(ScenarioBinaryXmlPath))
        {
            return ScenarioBinaryXmlPath;
        }

        var inputPath = HasScenarioBinary ? ScenarioBinaryPath : ExtractScenarioXml();
        var outputPath = inputPath.Replace(".bin", ".bin.xml");

        await Serz.Convert(inputPath);

        return outputPath;
    }

    public async Task<IHtmlDocument> GetXmlDocument()
    {
        var path = await ConvertBinToXml();
        var text = await File.ReadAllTextAsync(path);
        var document = await new HtmlParser().ParseDocumentAsync(text);

        XmlException.ThrowIfNotExists(document, path);

        return document;
    }

    private string ExtractScenarioXml()
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

    public async Task<string> ConvertXmlToBin()
    {
        var path = Path.Join(DirectoryPath, "Scenario.bin.xml");

        await Serz.Convert(path);

        return ScenarioBinaryPath;
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
