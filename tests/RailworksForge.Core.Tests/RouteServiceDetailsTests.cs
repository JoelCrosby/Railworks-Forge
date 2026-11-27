using System.IO.Compression;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Packaging;
using RailworksForge.Core.Routes;

namespace RailworksForge.Core.Tests;

public sealed class RouteServiceDetailsTests : IDisposable
{
    private const string PropertiesXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <cRouteProperties xmlns:d="http://www.kuju.com/TnT/2003/Delta" d:version="1.0">
            <ID><cGUID><DevString d:type="cDeltaString">route</DevString></cGUID></ID>
            <DisplayName>
                <Localisation-cUserLocalisedString>
                    <English d:type="cDeltaString">Tirol Austria</English>
                    <German d:type="cDeltaString">Tirol Österreich</German>
                    <Other/>
                    <Key d:type="cDeltaString">851655f5-aec6-4434-bdee-00e7bdafc9e2</Key>
                </Localisation-cUserLocalisedString>
            </DisplayName>
            <Locked d:type="bool">0</Locked>
        </cRouteProperties>
        """;

    private readonly DirectoryInfo _routeDirectory = Directory.CreateTempSubdirectory("railworks-route-");
    private readonly RouteService _service = new();

    public void Dispose()
    {
        _routeDirectory.Delete(recursive: true);
    }

    [Fact]
    public async Task UpdateDetails_WritesALoosePropertiesFileBesideAPackedRoute()
    {
        var route = CreatePackedRoute();
        var details = await _service.GetDetails(route);
        var renamed = details with { Name = details.Name.With(GameLanguage.English, "Tirol: Innsbruck - Kufstein") };

        var updated = await _service.UpdateDetails(route, renamed);

        var loosePath = Path.Join(_routeDirectory.FullName, "RouteProperties.xml");
        var archived = Archives.GetTextFileContentFromPath(route.MainContentArchivePath, "/RouteProperties.xml");
        var reread = await _service.GetDetails(route);

        Assert.True(File.Exists(loosePath));
        Assert.Equal("Tirol: Innsbruck - Kufstein", updated.Name);
        Assert.Equal("Tirol: Innsbruck - Kufstein", reread.Name[GameLanguage.English]);
        Assert.Equal("Tirol Österreich", reread.Name[GameLanguage.German]);
        Assert.Contains("<English d:type=\"cDeltaString\">Tirol Austria</English>", archived);
    }

    [Fact]
    public async Task UpdateDetails_InvalidatesTheRouteList()
    {
        var route = CreatePackedRoute();
        var details = await _service.GetDetails(route);
        var invalidated = false;
        _service.RoutesInvalidated += () => invalidated = true;

        await _service.UpdateDetails(route, details);

        Assert.True(invalidated);
    }

    [Fact]
    public async Task WritePropertiesDocument_ReplacesALooseFileWhoseNameDiffersInCase()
    {
        var lowercasePath = Path.Join(_routeDirectory.FullName, "routeproperties.xml");
        File.WriteAllText(lowercasePath, PropertiesXml);
        var route = CreatePackedRoute();
        var document = XmlParser.ParseDocument(PropertiesXml);
        var renamed = RouteDetails.Read(document) with { Name = LocalisedText.From((GameLanguage.English, "Tirol West")) };
        renamed.Write(document);

        await RouteWriter.WritePropertiesDocument(route, document);

        var looseFiles = Directory.GetFiles(_routeDirectory.FullName, "*.xml");
        var written = File.ReadAllText(lowercasePath);

        Assert.Equal([lowercasePath], looseFiles);
        Assert.Contains("Tirol West", written);
    }

    private Route CreatePackedRoute()
    {
        var archivePath = Path.Join(_routeDirectory.FullName, "MainContent.ap");

        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("RouteProperties.xml");
            using var writer = new StreamWriter(entry.Open());

            writer.Write(PropertiesXml);
        }

        return new Route
        {
            Id = _routeDirectory.Name,
            Name = "Tirol Austria",
            RoutePropertiesPath = archivePath,
            DirectoryPath = _routeDirectory.FullName,
            PackagingType = PackagingType.Packed,
        };
    }
}
