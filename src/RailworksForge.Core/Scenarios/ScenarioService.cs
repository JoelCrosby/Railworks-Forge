using System.IO.Compression;
using System.Runtime.ExceptionServices;

using AngleSharp.Dom;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Routes;

namespace RailworksForge.Core.Scenarios;

public class ScenarioService : IScenarioService
{
    private readonly IScenarioDatabaseService _scenarioDatabaseService;

    public ScenarioService(IScenarioDatabaseService scenarioDatabaseService)
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

    public IScenarioEditor CreateEditor(Scenario scenario)
    {
        return new ScenarioEditor(scenario);
    }

    // Scenarios are parsed in parallel, as parsing their properties is most of the work. Loose scenarios are added
    // first, so an edited copy wins over the packed original it was extracted from.
    public List<Scenario> GetScenarios(Route route, CancellationToken cancellationToken = default)
    {
        var scenarios = new HashSet<Scenario>();

        scenarios.UnionWith(ReadUnpackedScenarios(route, cancellationToken));
        scenarios.UnionWith(ReadPackedScenarios(route, cancellationToken));

        return scenarios.OrderBy(scenario => scenario.Name).ToList();
    }

    private List<Scenario> ReadPackedScenarios(Route route, CancellationToken cancellationToken)
    {
        var entries = Directory
            .EnumerateFiles(route.DirectoryPath, "*.ap")
            .SelectMany(package => ReadPropertiesEntries(package, cancellationToken))
            .ToList();

        return ReadInParallel(entries, entry => Scenario.New(route, entry.Path, () => ParseEntry(entry.Content)), cancellationToken);
    }

    // A ZipArchive can't be read from several threads, so the entries are decompressed up front and parsed in parallel.
    private static List<(AssetPath Path, byte[] Content)> ReadPropertiesEntries(string package, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(package);

        return archive.Entries
            .Where(entry => entry.Name == "ScenarioProperties.xml")
            .Select(entry =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var path = new AssetPath
                {
                    Path = package,
                    IsArchivePath = true,
                    ArchivePath = entry.FullName,
                };

                return (path, ReadEntry(entry));
            })
            .ToList();
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        using var content = entry.Open();
        using var buffer = new MemoryStream();

        content.CopyTo(buffer);

        return buffer.ToArray();
    }

    private static IDocument ParseEntry(byte[] content)
    {
        using var stream = new MemoryStream(content);

        return XmlParser.ParseDocument(stream);
    }

    private List<Scenario> ReadUnpackedScenarios(Route route, CancellationToken cancellationToken)
    {
        if (GetScenarioDirectory(route) is not {} directory) return [];

        var propertiesPaths = Directory
            .EnumerateDirectories(directory)
            .Select(scenarioDirectory => Path.Join(scenarioDirectory, "ScenarioProperties.xml"))
            .ToList();

        return ReadInParallel(propertiesPaths, ReadUnpackedScenario, cancellationToken);

        Scenario? ReadUnpackedScenario(string propertiesPath)
        {
            if (!Paths.Exists(propertiesPath)) return null;

            return Scenario.New(route, new AssetPath { Path = propertiesPath });
        }
    }

    private List<Scenario> ReadInParallel<T>(List<T> sources, Func<T, Scenario?> read, CancellationToken cancellationToken)
    {
        List<Scenario> scenarios;

        try
        {
            scenarios = sources
                .AsParallel()
                .AsOrdered()
                .WithCancellation(cancellationToken)
                .Select(read)
                .OfType<Scenario>()
                .ToList();
        }
        catch (AggregateException exception) when (exception.InnerExceptions.Count == 1)
        {
            // Rethrown unwrapped, so the error shown is the scenario's own rather than "One or more errors occurred".
            ExceptionDispatchInfo.Throw(exception.InnerExceptions[0]);
            throw;
        }

        foreach (var scenario in scenarios)
        {
            scenario.SetPlayerInfo(_scenarioDatabaseService.GetScenario(scenario.Id));
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
