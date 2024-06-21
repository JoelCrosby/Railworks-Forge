using System.IO.Compression;

using RailworksForge.Core.Models;

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
            foreach (var scenario in ReadCompressedScenarios(route, package))
            {
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

            if (!File.Exists(scenarioPath)) continue;

            var content = File.ReadAllText(scenarioPath);
            var scenario = ReadScenarioProperties(route, scenarioPath, content);

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

    private static IEnumerable<Scenario> ReadCompressedScenarios(Route route, string path)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Read);

        var entries = archive.Entries.Where(entry => entry.Name == "ScenarioProperties.xml");

        foreach (var entry in entries)
        {
            if (entry is null)
            {
                throw new Exception("failed to find route properties file in compressed archive");
            }

            var content = entry.Open();

            using var reader = new StreamReader(content);

            var file = reader.ReadToEnd();

            yield return ReadScenarioProperties(route, path, file);
        }
    }

    private static Scenario ReadScenarioProperties(Route route, string path, string fileContent)
    {
        var doc = XmlParser.ParseDocument(fileContent);

        return Scenario.Parse(doc.DocumentElement, route, path);
    }
}
