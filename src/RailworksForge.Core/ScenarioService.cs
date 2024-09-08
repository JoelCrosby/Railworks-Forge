using System.IO.Compression;

using RailworksForge.Core.Models;
using RailworksForge.Core.Types;

namespace RailworksForge.Core;

public static class ScenarioService
{
    public static List<Scenario> GetScenarios(Route route)
    {
        var scenarios = new HashSet<Scenario>();

        AddUnPackedScenarios(route, scenarios);
        AddPackedScenarios(route, scenarios);

        return scenarios.OrderBy(scenario => scenario.Name).ToList();
    }

    private static void AddPackedScenarios(Route route, HashSet<Scenario> scenarios)
    {
        foreach (var package in Directory.EnumerateFiles(route.DirectoryPath, "*.ap"))
        {
            foreach (var path in ReadCompressedScenarios(package))
            {
                var scenario = Scenario.New(route, path);

                if (scenario is null) continue;

                scenarios.Add(scenario);
            }
        }
    }

    private static void AddUnPackedScenarios(Route route, HashSet<Scenario> scenarios)
    {
        if (GetScenarioDirectory(route) is not {} dir) return;

        foreach (var scenario in ReadScenarioFiles(route, dir))
        {
            scenarios.Add(scenario);
        }
    }

    private static List<Scenario> ReadScenarioFiles(Route route, string directory)
    {
        var scenarios = new List<Scenario>();

        foreach (var scenarioDir in Directory.EnumerateDirectories(directory))
        {
            var scenarioPath = Path.Join(scenarioDir, "ScenarioProperties.xml");

            if (!Paths.Exists(scenarioPath)) continue;

            var scenario = Scenario.New(route, new AssetPath { Path = scenarioPath });

            if (scenario is null) continue;

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
        });
    }
}
