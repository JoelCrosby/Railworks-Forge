using AngleSharp.Dom;

using RailworksForge.Core.Common;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Tests;

public class ScenarioDetailsTests
{
    private const string PropertiesXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <cScenarioProperties xmlns:d="http://www.kuju.com/TnT/2003/Delta" d:version="1.0" d:id="192482524">
            <ID><cGUID><DevString d:type="cDeltaString">6953d549</DevString></cGUID></ID>
            <String/>
            <DisplayName>
                <Localisation-cUserLocalisedString>
                    <English d:type="cDeltaString">IR 1831: Dej Calatori</English>
                    <French d:type="cDeltaString"></French>
                    <Italian d:type="cDeltaString"></Italian>
                    <Spanish d:type="cDeltaString"></Spanish>
                    <Other>
                        <Localisation-cUserLocalisedString-cOtherStringLangPair>
                            <Language d:type="cDeltaString">Romanian</Language>
                            <String d:type="cDeltaString">IR 1831: Dej Călători</String>
                        </Localisation-cUserLocalisedString-cOtherStringLangPair>
                    </Other>
                    <Key d:type="cDeltaString">b390533a-8ffc-4184-a1ab-e8244a9c3940</Key>
                </Localisation-cUserLocalisedString>
            </DisplayName>
            <Briefing>
                <Localisation-cUserLocalisedString>
                    <English d:type="cDeltaString">&lt;supply briefing&gt;</English>
                    <Key d:type="cDeltaString">df99cf50-cae3-43b2-ad25-2bde2cedcab5</Key>
                </Localisation-cUserLocalisedString>
            </Briefing>
            <StartLocation>
                <Localisation-cUserLocalisedString>
                    <English d:type="cDeltaString">Dej Calatori</English>
                    <Key d:type="cDeltaString">8622b558-38eb-40e9-8f27-e94b210c7c79</Key>
                </Localisation-cUserLocalisedString>
            </StartLocation>
            <ScenarioClass d:type="cDeltaString">eStandardScenarioClass</ScenarioClass>
            <StartTime d:type="sFloat32" d:alt_encoding="00000000009CE840" d:precision="string">50400.0000</StartTime>
            <DurationMins d:type="sInt32">50</DurationMins>
            <StartDD d:type="sInt32">1</StartDD>
            <StartMM d:type="sInt32">7</StartMM>
            <StartYYYY d:type="sInt32">2010</StartYYYY>
            <Season d:type="cDeltaString">SEASON_SUMMER</Season>
            <Rating d:type="sInt32">4</Rating>
            <FrontEndDriverList>
                <sDriverFrontEndDetails>
                    <StartLocation>
                        <Localisation-cUserLocalisedString>
                            <English d:type="cDeltaString">Beclean pe Somes</English>
                        </Localisation-cUserLocalisedString>
                    </StartLocation>
                </sDriverFrontEndDetails>
            </FrontEndDriverList>
            <WorkshopId d:type="sUInt64">0</WorkshopId>
        </cScenarioProperties>
        """;

    [Fact]
    public void Read_ReadsTheScenarioDetails()
    {
        var details = ScenarioDetails.Read(Parse(PropertiesXml));

        Assert.Equal("IR 1831: Dej Calatori", details.Name[GameLanguage.English]);
        Assert.Equal("Dej Calatori", details.StartLocation.DisplayText);
        Assert.Equal("<supply briefing>", details.Briefing[GameLanguage.English]);
        Assert.False(details.Description.HasAnyText);
        Assert.Equal(50, details.DurationMinutes);
        Assert.Equal(4, details.Rating);
        Assert.Equal(ScenarioSeason.Summer, details.Season);
        Assert.Equal(new DateOnly(2010, 7, 1), details.StartDate);
        Assert.Equal(TimeSpan.FromHours(14), details.StartTime);
        Assert.Equal(ScenarioClass.Standard, details.ScenarioClass);
        Assert.False(details.IsWorkshopItem);
    }

    [Fact]
    public void Write_UpdatesALanguageAndKeepsTheKeyAndOtherLanguages()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var edited = original with { Name = original.Name.With(GameLanguage.French, "IR 1831 : Dej") };

        edited.Write(document, original);

        var reread = Reparse(document);
        var name = reread.QuerySelector("DisplayName")!;

        Assert.Equal("IR 1831 : Dej", ScenarioDetails.Read(reread).Name[GameLanguage.French]);
        Assert.Equal("IR 1831: Dej Calatori", ScenarioDetails.Read(reread).Name[GameLanguage.English]);
        Assert.Equal("b390533a-8ffc-4184-a1ab-e8244a9c3940", name.QuerySelector("Key")!.TextContent);
        Assert.Equal("IR 1831: Dej Călători", name.QuerySelector("Other String")!.TextContent);
    }

    [Fact]
    public void Write_AddsAMissingLanguageSlotInGameOrder()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var edited = original with { Name = original.Name.With(GameLanguage.German, "IR 1831: Dej Bahnhof") };

        edited.Write(document, original);

        var reread = Reparse(document);
        var slots = reread.QuerySelector("DisplayName Localisation-cUserLocalisedString")!.Children.Select(c => c.LocalName);
        var german = reread.QuerySelector("DisplayName German")!;

        Assert.Equal(["English", "French", "Italian", "German", "Spanish", "Other", "Key"], slots);
        Assert.Equal("cDeltaString", german.GetAttribute(Utilities.NS, "type"));
        Assert.Equal("IR 1831: Dej Bahnhof", german.TextContent);
    }

    [Fact]
    public void Write_CreatesAMissingFieldAfterThePrecedingOne()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var edited = original with { Description = LocalisedText.From((GameLanguage.English, "Morning stopper")) };

        edited.Write(document, original);

        var reread = Reparse(document);
        var fields = reread.DocumentElement.Children.Select(child => child.LocalName).ToList();
        var key = reread.QuerySelector("Description Key")!.TextContent;

        Assert.Equal(fields.IndexOf("DisplayName") + 1, fields.IndexOf("Description"));
        Assert.Equal("Morning stopper", ScenarioDetails.Read(reread).Description[GameLanguage.English]);
        Assert.True(Guid.TryParse(key, out _));
    }

    [Fact]
    public void Write_KeepsMarkupCharactersAndLineBreaks()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var briefing = "Couple to the <b>wagons</b> & depart.\nSignal 12 is at danger.";
        var edited = original with { Briefing = original.Briefing.With(GameLanguage.English, briefing) };

        edited.Write(document, original);

        Assert.Equal(briefing, ScenarioDetails.Read(Reparse(document)).Briefing[GameLanguage.English]);
    }

    [Fact]
    public void Write_LeavesTheServiceStartLocationsAlone()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var edited = original with { StartLocation = LocalisedText.From((GameLanguage.English, "Dej Triaj")) };

        edited.Write(document, original);

        var reread = Reparse(document);

        Assert.Equal("Dej Triaj", ScenarioDetails.Read(reread).StartLocation.DisplayText);
        Assert.Equal("Beclean pe Somes", reread.QuerySelector("sDriverFrontEndDetails StartLocation English")!.TextContent);
    }

    [Fact]
    public void Write_EncodesTheStartTimeBits()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var edited = original with { StartTime = new TimeSpan(14, 14, 0) };

        edited.Write(document, original);

        var startTime = Reparse(document).DocumentElement.ChildElement("StartTime")!;

        Assert.Equal("51240", startTime.TextContent);
        Assert.Equal("000000000005E940", startTime.GetAttribute(Utilities.NS, "alt_encoding"));
    }

    [Fact]
    public void Write_OnlyTouchesChangedFields()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var edited = original with { DurationMinutes = 75 };

        edited.Write(document, original);

        var root = Reparse(document).DocumentElement;

        Assert.Equal("75", root.ChildElement("DurationMins")!.TextContent);
        Assert.Equal("50400.0000", root.ChildElement("StartTime")!.TextContent);
    }

    [Fact]
    public void Write_UpdatesTheSeasonAndStartDate()
    {
        var document = Parse(PropertiesXml);
        var original = ScenarioDetails.Read(document);
        var edited = original with { Season = ScenarioSeason.Winter, StartDate = new DateOnly(1985, 12, 24), Rating = 2 };

        edited.Write(document, original);

        var reread = ScenarioDetails.Read(Reparse(document));

        Assert.Equal(ScenarioSeason.Winter, reread.Season);
        Assert.Equal(new DateOnly(1985, 12, 24), reread.StartDate);
        Assert.Equal(2, reread.Rating);
    }

    [Fact]
    public void Equality_ComparesTheText()
    {
        var first = ScenarioDetails.Read(Parse(PropertiesXml));
        var second = ScenarioDetails.Read(Parse(PropertiesXml));

        Assert.Equal(first, second);
        Assert.NotEqual(first, second with { Name = second.Name.With(GameLanguage.Dutch, "IR 1831") });
    }

    [Fact]
    public void ApplyTo_ShowsTheEditsOnTheScenario()
    {
        var details = ScenarioDetails.Read(Parse(PropertiesXml)) with
        {
            Name = LocalisedText.From((GameLanguage.German, "Nach Beclean")),
            Season = ScenarioSeason.Autumn,
        };

        var scenario = details.ApplyTo(DesignScenario());

        Assert.Equal("Nach Beclean", scenario.Name);
        Assert.Equal("nach beclean", scenario.SearchIndex);
        Assert.Equal("Autumn", scenario.Season);
    }

    [Fact]
    public void RouteDetails_ReadsLockedAndWorkshopRoutes()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <cRouteProperties xmlns:d="http://www.kuju.com/TnT/2003/Delta" d:version="1.0">
                <ID><cGUID><DevString d:type="cDeltaString">fc8df6e1</DevString></cGUID></ID>
                <DisplayName>
                    <Localisation-cUserLocalisedString>
                        <English d:type="cDeltaString">Tirol Austria</English>
                        <Key d:type="cDeltaString">851655f5</Key>
                    </Localisation-cUserLocalisedString>
                </DisplayName>
                <Locked d:type="bool">1</Locked>
                <AuthoredLanguage d:type="cDeltaString">de</AuthoredLanguage>
                <WorkshopId d:type="sUInt64">2918273645</WorkshopId>
            </cRouteProperties>
            """;

        var details = RouteDetails.Read(Parse(xml));

        Assert.Equal("Tirol Austria", details.Name.DisplayText);
        Assert.Equal("de", details.AuthoredLanguage);
        Assert.True(details.IsLocked);
        Assert.True(details.IsWorkshopItem);
    }

    private static Scenario DesignScenario()
    {
        var route = new Route
        {
            Id = "route",
            Name = "Route",
            RoutePropertiesPath = "/route/RouteProperties.xml",
            DirectoryPath = "/route",
            PackagingType = Packaging.PackagingType.Unpacked,
        };

        return new Scenario
        {
            Id = "scenario",
            Route = route,
            Name = "Old",
            Locomotive = "060-EA",
            Duration = 50,
            Rating = 4,
            DirectoryPath = "/route/Scenarios/scenario",
            ScenarioPropertiesPath = "/route/Scenarios/scenario/ScenarioProperties.xml",
            AssetPath = new Assets.AssetPath { Path = "/route/Scenarios/scenario/ScenarioProperties.xml" },
        };
    }

    private static IDocument Parse(string xml)
    {
        return XmlParser.ParseDocument(xml);
    }

    private static IDocument Reparse(IDocument document)
    {
        var directory = Directory.CreateTempSubdirectory("railworks-details-");
        var path = Path.Join(directory.FullName, "ScenarioProperties.xml");

        try
        {
            document.ToXmlAsync(path).GetAwaiter().GetResult();

            return Parse(File.ReadAllText(path));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
