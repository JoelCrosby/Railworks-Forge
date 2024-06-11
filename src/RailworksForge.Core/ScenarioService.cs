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
        var scenarios = new List<Scenario>();

        if (GetScenarioDirectory(route) is {} dir)
        {
            scenarios.AddRange(ReadScenarioFiles(dir));
        }

        foreach (var package in Directory.EnumerateFiles(route.Path, "*.ap"))
        {
            scenarios.AddRange(ReadCompressedScenarios(package));
        }

        return scenarios.OrderBy(scenario => scenario.Name).ToList();
    }

    public static async Task<ScenarioTree> GetScenarioTree(Scenario scenario)
    {
        var path = await scenario.ConvertBinToXml();
        var content = await File.ReadAllTextAsync(path);

        var doc = new HtmlParser().ParseDocument(content);

        if (doc.Body is null)
        {
            return new ScenarioTree();
        }

        var root = new ScenarioTree
        {
            Nodes = AddChildren(doc.Body),
        };

        return root;

        List<ScenarioTreeNode> AddChildren(IElement element)
        {
            var nodes = new List<ScenarioTreeNode>();

            foreach (var child in element.Children)
            {
                nodes.Add(new ScenarioTreeNode
                {
                    Name = child.NodeName,
                    Content = GetTextContent(),
                    ChildNodes = AddChildren(child),
                });

                continue;

                string? GetTextContent()
                {
                    return child.ChildNodes
                        .FirstOrDefault(c => c.NodeType == NodeType.Text)?
                        .TextContent;
                }
            }

            return nodes;
        }
    }

    private static List<Scenario> ReadScenarioFiles(string directory)
    {
        var scenarios = new List<Scenario>();

        foreach (var scenarioDir in Directory.EnumerateDirectories(directory))
        {
            var scenarioPath = Path.Join(scenarioDir, "ScenarioProperties.xml");

            if (!File.Exists(scenarioPath)) continue;

            var content = File.ReadAllText(scenarioPath);
            var scenario = ReadScenarioProperties(scenarioPath, content);

            scenarios.Add(scenario);
        }

        return scenarios;
    }

    private static string? GetScenarioDirectory(Route route)
    {
        return Directory.EnumerateDirectories(route.Path).FirstOrDefault(path =>
        {
            var dirname = Path.GetFileName(path);
            return string.Equals(dirname, "Scenarios", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static IEnumerable<Scenario> ReadCompressedScenarios(string path)
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

            yield return ReadScenarioProperties(path, file);
        }
    }

    private static Scenario ReadScenarioProperties(string path, string fileContent)
    {
        var id = Directory.GetParent(path)?.Name ?? string.Empty;
        var doc = new HtmlParser().ParseDocument(fileContent);
        var name = doc.SelectTextContnet("DisplayName English");
        var description = doc.SelectTextContnet("description English");
        var briefing = doc.SelectTextContnet("Briefing English");
        var startLocation = doc.SelectTextContnet("StartLocation English");
        var directoryPath = Directory.GetParent(path)?.FullName ?? string.Empty;
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
            Path = directoryPath,
            RootPath = path,
            Consists = consists,
            ScenarioClass = ScenarioClassTypes.Parse(scenarioClass),
            PackagingType = path.EndsWith(".xml") ? PackagingType.Unpacked : PackagingType.Packed,
            FileContent = fileContent,
        };
    }

    private static Consist ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContnet("LocoName English");
        var serviceName = el.SelectTextContnet("ServiceName English");
        var playerDriver = el.SelectTextContnet("PlayerDriver") == "1";

        return new Consist
        {
            Id = consistId,
            LocomotiveName = locomotiveName,
            ServiceName = serviceName,
            PlayerDriver = playerDriver,
            RawText = el.OuterHtml,
        };
    }
}
