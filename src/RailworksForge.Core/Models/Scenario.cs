namespace RailworksForge.Core.Models;

public record Scenario
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Briefing { get; init; }

    public string? StartLocation { get; init; }

    public string? Locomotive { get; init; }

    public string? Path { get; init; }

    public string? RootPath { get; init; }

    public string? FileContent { get; init; }

    public List<Consist> Consists { get; init; } = [];

    public required PackagingType PackagingType { get; init; }

    public required ScenarioClass ScenarioClass { get; init; }

    private string ScenarioBinaryPath => System.IO.Path.Join(Path, "Scenario.bin");
    private string ScenarioBinaryXmlPath => System.IO.Path.Join(Path, "Scenario.bin.xml");

    public async Task<string> ConvertBinToXml()
    {
        if (File.Exists(ScenarioBinaryXmlPath))
        {
            return ScenarioBinaryXmlPath;
        }

        var path = System.IO.Path.Join(Path, "Scenario.bin");

        await Serz.Convert(path);

        return ScenarioBinaryXmlPath;
    }

    public async Task<string> ConvertXmlToBin()
    {
        var path = System.IO.Path.Join(Path, "Scenario.bin.xml");

        await Serz.Convert(path);

        return ScenarioBinaryPath;
    }
}
