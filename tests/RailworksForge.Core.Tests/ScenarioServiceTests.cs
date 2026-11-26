using System.IO.Compression;

using RailworksForge.Core.Packaging;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Tests;

public sealed class ScenarioServiceTests : IDisposable
{
    private readonly DirectoryInfo _routeDirectory = Directory.CreateTempSubdirectory("railworks-scenarios-");
    private readonly ScenarioService _service = new(new EmptyScenarioDatabase());

    public void Dispose()
    {
        _routeDirectory.Delete(recursive: true);
    }

    [Fact]
    public void GetScenarios_ReadsLooseScenariosInNameOrder()
    {
        WriteLooseScenario("b-id", "Bravo", "ScenarioProperties.xml");
        WriteLooseScenario("a-id", "Alpha", "ScenarioProperties.xml");

        var scenarios = _service.GetScenarios(CreateRoute());

        Assert.Equal(["Alpha", "Bravo"], scenarios.Select(scenario => scenario.Name));
    }

    [Fact]
    public void GetScenarios_ReadsPropertiesWhoseNameDiffersInCase()
    {
        WriteLooseScenario("a-id", "Alpha", "scenarioproperties.xml");

        var scenario = Assert.Single(_service.GetScenarios(CreateRoute()));

        Assert.Equal("Alpha", scenario.Name);
    }

    [Fact]
    public void GetScenarios_ReadsPackedScenarios()
    {
        WritePackedScenarios(("a-id", "Alpha"), ("b-id", "Bravo"));

        var scenarios = _service.GetScenarios(CreateRoute());

        Assert.Equal(["Alpha", "Bravo"], scenarios.Select(scenario => scenario.Name));
        Assert.All(scenarios, scenario => Assert.Equal(PackagingType.Packed, scenario.PackagingType));
    }

    [Fact]
    public void GetScenarios_PrefersAnEditedCopyOverThePackedOriginal()
    {
        WritePackedScenarios(("a-id", "Packed"));
        WriteLooseScenario("a-id", "Edited", "ScenarioProperties.xml");

        var scenario = Assert.Single(_service.GetScenarios(CreateRoute()));

        Assert.Equal("Edited", scenario.Name);
        Assert.Equal(PackagingType.Unpacked, scenario.PackagingType);
    }

    [Fact]
    public void GetScenarios_ThrowsTheScenarioErrorUnwrapped()
    {
        WriteLooseScenario("a-id", "Alpha", "ScenarioProperties.xml");
        var looseDirectory = Path.Join(_routeDirectory.FullName, "Scenarios", "b-id");
        Directory.CreateDirectory(Path.Join(looseDirectory, "ScenarioProperties.xml"));

        var exception = Record.Exception(() => _service.GetScenarios(CreateRoute()));

        Assert.NotNull(exception);
        Assert.IsNotType<AggregateException>(exception);
    }

    private Route CreateRoute()
    {
        return new Route
        {
            Id = _routeDirectory.Name,
            Name = "Route",
            RoutePropertiesPath = Path.Join(_routeDirectory.FullName, "RouteProperties.xml"),
            DirectoryPath = _routeDirectory.FullName,
            PackagingType = PackagingType.Unpacked,
        };
    }

    private void WriteLooseScenario(string id, string name, string filename)
    {
        var directory = Directory.CreateDirectory(Path.Join(_routeDirectory.FullName, "Scenarios", id));

        File.WriteAllText(Path.Join(directory.FullName, filename), PropertiesXml(id, name));
    }

    private void WritePackedScenarios(params (string Id, string Name)[] scenarios)
    {
        var archivePath = Path.Join(_routeDirectory.FullName, "MainContent.ap");
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

        foreach (var (id, name) in scenarios)
        {
            var entry = archive.CreateEntry($"Scenarios/{id}/ScenarioProperties.xml");
            using var writer = new StreamWriter(entry.Open());

            writer.Write(PropertiesXml(id, name));
        }
    }

    private static string PropertiesXml(string id, string name)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <cScenarioProperties xmlns:d="http://www.kuju.com/TnT/2003/Delta" d:version="1.0">
                <ID><cGUID><DevString d:type="cDeltaString">{id}</DevString></cGUID></ID>
                <DisplayName>
                    <Localisation-cUserLocalisedString>
                        <English d:type="cDeltaString">{name}</English>
                    </Localisation-cUserLocalisedString>
                </DisplayName>
            </cScenarioProperties>
            """;
    }

    private sealed class EmptyScenarioDatabase : IScenarioDatabaseService
    {
        public event Action? Updated
        {
            add { }
            remove { }
        }

        public Task Start(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ScenarioPlayerInfo GetScenario(string id) => ScenarioPlayerInfo.Empty;
    }
}
