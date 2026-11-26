using System.IO.Compression;

using AngleSharp.Dom;
using AngleSharp.Xml;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Routes;

namespace RailworksForge.Core.Scenarios;

public class ScenarioService
{
    private readonly ScenarioDatabaseService _scenarioDatabaseService;

    public ScenarioService(ScenarioDatabaseService  scenarioDatabaseService)
    {
        _scenarioDatabaseService = scenarioDatabaseService;
    }

    public event Action? PlayerInfoUpdated
    {
        add => _scenarioDatabaseService.Updated += value;
        remove => _scenarioDatabaseService.Updated -= value;
    }

    public void RefreshPlayerInfo(IEnumerable<Scenario> scenarios)
    {
        foreach (var scenario in scenarios)
        {
            scenario.SetPlayerInfo(_scenarioDatabaseService.GetScenario(scenario.Id));
        }
    }

    // Returns the path of the exported XML file.
    public Task<string> ExportBinToXml(Scenario scenario)
    {
        return scenario.ExportBinToXml();
    }

    public Task<string> ConvertXmlToBin(Scenario scenario)
    {
        return scenario.ConvertXmlToBin();
    }

    // Unpacks the route's archived scenarios so they can be edited as loose files.
    public void ExtractScenarios(Route route)
    {
        route.ExtractScenarios();
    }

    public List<Scenario> GetScenarios(Route route, CancellationToken cancellationToken = default)
    {
        var scenarios = new HashSet<Scenario>();

        AddUnPackedScenarios(route, scenarios, cancellationToken);
        AddPackedScenarios(route, scenarios, cancellationToken);

        return scenarios.OrderBy(scenario => scenario.Name).ToList();
    }

    private void AddPackedScenarios(Route route, HashSet<Scenario> scenarios, CancellationToken cancellationToken)
    {
        foreach (var package in Directory.EnumerateFiles(route.DirectoryPath, "*.ap"))
        {
            using var archive = ZipFile.OpenRead(package);

            var propertiesEntries = archive.Entries.Where(entry => entry.Name == "ScenarioProperties.xml");

            foreach (var entry in propertiesEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var path = new AssetPath
                {
                    Path = package,
                    IsArchivePath = true,
                    ArchivePath = entry.FullName,
                };

                var scenario = Scenario.New(route, path, () => ParseEntry(entry));

                if (scenario is null)
                {
                    continue;
                }

                scenario.SetPlayerInfo(_scenarioDatabaseService.GetScenario(scenario.Id));
                scenarios.Add(scenario);
            }
        }
    }

    private static IDocument ParseEntry(ZipArchiveEntry entry)
    {
        using var content = entry.Open();

        return XmlParser.ParseDocument(content);
    }

    private void AddUnPackedScenarios(Route route, HashSet<Scenario> scenarios, CancellationToken cancellationToken)
    {
        if (GetScenarioDirectory(route) is not {} dir) return;

        foreach (var scenario in ReadScenarioFiles(route, dir, cancellationToken))
        {
            scenarios.Add(scenario);
        }
    }

    private List<Scenario> ReadScenarioFiles(Route route, string directory, CancellationToken cancellationToken)
    {
        var scenarios = new List<Scenario>();

        foreach (var scenarioDir in Directory.EnumerateDirectories(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scenarioPath = Path.Join(scenarioDir, "ScenarioProperties.xml");

            if (!Paths.Exists(scenarioPath)) continue;

            var scenario = Scenario.New(route, new AssetPath { Path = scenarioPath });

            if (scenario is null) continue;

            scenario.SetPlayerInfo(_scenarioDatabaseService.GetScenario(scenario.Id));
            scenarios.Add(scenario);
        }

        return scenarios;
    }

    private static string? GetScenarioDirectory(Route route)
    {
        return Directory.EnumerateDirectories(route.DirectoryPath).FirstOrDefault(path =>
        {
            var dirname = Path.GetFileName(path);
            return string.Equals(dirname, "Scenarios", StringComparison.OrdinalIgnoreCase);
        });
    }
}
