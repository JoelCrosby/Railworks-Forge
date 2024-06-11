namespace RailworksForge.Core.Models;

public record Scenario
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string Briefing { get; init; }

    public required string StartLocation { get; init; }

    public required string Locomotive { get; init; }

    public required string Path { get; init; }

    public required string RootPath { get; init; }

    public required string FileContent { get; init; }

    public required List<Consist> Consists { get; init; }

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
