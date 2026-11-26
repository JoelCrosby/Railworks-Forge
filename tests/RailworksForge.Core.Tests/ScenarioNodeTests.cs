using AngleSharp.Dom;

using RailworksForge.Core.Common;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Tests;

public class ScenarioNodeTests
{
    [Fact]
    public void ReadConsists_FromBinaryMatchesTheXmlDocument()
    {
        var fromBinary = ScenarioNode.ReadConsists(GetResourceBytes("Scenario.bin"), CancellationToken.None);
        var fromDocument = ScenarioNode.ReadConsists(ParseResource("Scenario.bin.xml"));

        Assert.Equal(14, fromBinary.Count);
        Assert.Equal(Describe(fromDocument), Describe(fromBinary));
    }

    [Fact]
    public void ReadConsists_ParsesConsistsAndVehicles()
    {
        var consists = ScenarioNode.ReadConsists(GetResourceBytes("Scenario.bin"), CancellationToken.None);

        var first = Consist.ParseScenarioConsist(consists[0], 0)!;
        var vehicles = Scenario.GetServiceConsistVehicles(consists, first);

        Assert.Equal("547440632", first.Id);
        Assert.NotEmpty(vehicles);
        Assert.Equal("162682144", vehicles[0].Id);
        Assert.Equal("82718", vehicles[0].UniqueNumber);
        Assert.True(vehicles[0].Flipped);
        Assert.Equal("TTA Liquid Tank Green", vehicles[0].LocomotiveName);
        Assert.Equal("Kuju", vehicles[0].BlueprintSetIdProvider);
        Assert.Equal("0060de86-d41b-4673-9b18-ae1aff242d16", vehicles[0].EntityID);
    }

    [Fact]
    public void QuerySelector_MatchesDescendantChainsInDocumentOrder()
    {
        var consist = ParseConsist("""
            <cConsist d:id="1">
                <Other><Key>outside</Key></Other>
                <Driver>
                    <Inner><ServiceName><Key>first</Key></ServiceName></Inner>
                    <ServiceName><Key>second</Key></ServiceName>
                </Driver>
            </cConsist>
            """);

        Assert.Equal("first", consist.SelectTextContent("Driver ServiceName Key"));
        Assert.Equal(["first", "second"], consist.QuerySelectorAll("ServiceName Key").Select(node => node.TextContent));
        Assert.Equal("outside", consist.SelectTextContent("Key"));
        Assert.Equal(string.Empty, consist.SelectTextContent("Missing Key"));
    }

    [Fact]
    public void QuerySelector_MatchesAncestorsOutsideTheNode()
    {
        var consist = ParseConsist("""
            <cConsist d:id="1">
                <RailVehicles><cOwnedEntity d:id="2"><Name>Loco</Name></cOwnedEntity></RailVehicles>
            </cConsist>
            """);

        var vehicle = consist.QuerySelector("cOwnedEntity")!;

        Assert.Equal("2", vehicle.Id);
        Assert.Equal("Loco", vehicle.SelectTextContent("RailVehicles Name"));
    }

    [Fact]
    public void SelectLocalisedStringContent_ReturnsTheFirstTranslationWithText()
    {
        var consist = ParseConsist("""
            <cConsist d:id="1">
                <ServiceName>
                    <Localisation-cUserLocalisedString>
                        <English>  </English>
                        <French>
            Service</French>
                        <German>Dienst</German>
                    </Localisation-cUserLocalisedString>
                </ServiceName>
            </cConsist>
            """);

        Assert.Equal("Service", consist.SelectLocalisedStringContent("ServiceName"));
    }

    private static ScenarioNode ParseConsist(string consistXml)
    {
        var xml = $"""<cRecordSet xmlns:d="http://www.kuju.com/TnT/2003/Delta">{consistXml}</cRecordSet>""";

        return Assert.Single(ScenarioNode.ReadConsists(XmlParser.ParseDocument(xml)));
    }

    private static List<string> Describe(List<ScenarioNode> consists)
    {
        return consists
            .Select((node, index) => Consist.ParseScenarioConsist(node, index))
            .OfType<Consist>()
            .Select(consist =>
            {
                var vehicles = Scenario
                    .GetServiceConsistVehicles(consists, consist)
                    .Select(vehicle => $"{vehicle.Id} {vehicle.EntityID} {vehicle.SearchIndex} {vehicle.Flipped}");

                return $"{consist.Id} {consist.SearchIndex} {consist.PlayerDriver} {consist.Length} | {string.Join(" | ", vehicles)}";
            })
            .ToList();
    }

    private static IDocument ParseResource(string name)
    {
        using var stream = GetResource(name);

        return XmlParser.ParseDocument(stream);
    }

    private static byte[] GetResourceBytes(string name)
    {
        using var stream = GetResource(name);
        using var buffer = new MemoryStream();

        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    private static Stream GetResource(string name)
    {
        return typeof(ScenarioNodeTests).Assembly.GetManifestResourceStream($"RailworksForge.Core.Tests.Resources.{name}")!;
    }
}
