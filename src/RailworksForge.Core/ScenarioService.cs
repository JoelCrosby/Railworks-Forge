using System.IO.Compression;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using RailworksForge.Core.Extensions;
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
        var doc = new HtmlParser().ParseDocument(fileContent);

        var id = doc.SelectTextContnet("ID cGUID DevString");
        var name = doc.SelectTextContnet("DisplayName English");
        var description = doc.SelectTextContnet("description English");
        var briefing = doc.SelectTextContnet("Briefing English");
        var startLocation = doc.SelectTextContnet("StartLocation English");
        var directoryPath = Path.GetDirectoryName(path) ?? string.Empty;
        var scenarioClass = doc.SelectTextContnet("ScenarioClass");

        var consists = doc.QuerySelectorAll("sDriverFrontEndDetails").Select(ParseConsist).ToList();

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
            FileContent = fileContent,
            Route = route,
        };
    }

    private static Consist ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContnet("LocoName English");
        var serviceName = el.SelectTextContnet("ServiceName English");
        var playerDriver = el.SelectTextContnet("PlayerDriver") == "1";
        var locoAuthor = el.SelectTextContnet("LocoAuthor");
        var blueprintId = el.SelectTextContnet("BlueprintID");
        var serviceId = el.SelectTextContnet("ServiceName Key");
        var locoClass = LocoClassUtils.Parse(el.SelectTextContnet("LocoClass"));
        var blueprintSetIdProduct = el.SelectTextContnet("LocoBP iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContnet("LocoBP iBlueprintLibrary-cBlueprintSetID Provider");

        return new Consist
        {
            Id = consistId,
            LocomotiveName = locomotiveName,
            LocoAuthor = locoAuthor,
            LocoClass = locoClass,
            ServiceName = serviceName,
            PlayerDriver = playerDriver,
            BlueprintId = blueprintId,
            BlueprintSetIdProduct = blueprintSetIdProduct,
            BlueprintSetIdProvider = blueprintSetIdProvider,
            ServiceId = serviceId,
            RawText = el.OuterHtml,
        };
    }
}
