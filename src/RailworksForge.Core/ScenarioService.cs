using System.IO.Compression;

using AngleSharp.Xml;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Core.Types;

namespace RailworksForge.Core;

public record ScenarioConsists(Scenario Scenario, List<Consist> Consists);

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

    public List<Scenario> GetScenarios(Route route, CancellationToken cancellationToken = default)
    {
        var scenarios = new HashSet<Scenario>();

        AddUnPackedScenarios(route, scenarios, cancellationToken);
        AddPackedScenarios(route, scenarios, cancellationToken);

        return scenarios.OrderBy(scenario => scenario.Name).ToList();
    }

    public async Task<ScenarioConsists> LoadConsists(Scenario scenario, CancellationToken cancellationToken)
    {
        ClearAssetCaches();

        var updated = scenario.Refresh() ?? throw new InvalidOperationException("The scenario could not be loaded.");
        using var document = await updated.GetXmlDocument(false);

        cancellationToken.ThrowIfCancellationRequested();
        Cache.ConsistAcquisitionStates.Clear();

        var consists = document
            .QuerySelectorAll("cConsist")
            .Select(Consist.ParseScenarioConsist)
            .OfType<Consist>()
            .ToList();

        return new ScenarioConsists(updated, consists);
    }

    public async Task<List<ConsistRailVehicle>> GetConsistVehicles(
        Scenario scenario,
        Consist consist,
        CancellationToken cancellationToken)
    {
        ClearAssetCaches();

        if (string.IsNullOrWhiteSpace(consist.BlueprintId))
        {
            return [];
        }

        var vehicles = await scenario.GetServiceConsistVehicles(consist);

        // Acquisition state is computed lazily from disk; resolve it here so the grid doesn't do it on the UI thread.
        foreach (var vehicle in vehicles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = vehicle.AcquisitionState;
        }

        return vehicles;
    }

    public async Task<string> GetConsistRailVehiclesXml(Scenario scenario, Consist consist)
    {
        using var document = await scenario.GetXmlDocument();

        var vehicles = document
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("ServiceName Key", consist.ServiceId)?
            .QuerySelector("RailVehicles");

        if (vehicles is null)
        {
            throw new Exception("could not find consist in scenario bin");
        }

        return vehicles.ToXml();
    }

    private static void ClearAssetCaches()
    {
        Cache.BlueprintAcquisitionStates.Clear();
        Cache.ArchiveCache.Clear();
    }

    private void AddPackedScenarios(Route route, HashSet<Scenario> scenarios, CancellationToken cancellationToken)
    {
        foreach (var package in Directory.EnumerateFiles(route.DirectoryPath, "*.ap"))
        {
            foreach (var path in ReadCompressedScenarios(package))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var scenario = Scenario.New(route, path);

                if (scenario is null) continue;

                scenario.SetPlayerInfo(_scenarioDatabaseService.GetScenario(scenario.Id));
                scenarios.Add(scenario);
            }
        }
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

    private static IEnumerable<AssetPath> ReadCompressedScenarios(string path)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Read);

        var entries = archive.Entries.Where(entry => entry.Name == "ScenarioProperties.xml");

        return entries.Select(e => new AssetPath
        {
            Path = path,
            IsArchivePath = true,
            ArchivePath = e.FullName,
        }).ToList();
    }
}
