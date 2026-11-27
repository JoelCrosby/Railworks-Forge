using AngleSharp.Dom;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Consists.Commands;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Packaging;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Tests;

public class ConsistVehicleCommandTests
{
    private const string ServiceConsistId = "345408992";

    [Fact]
    public async Task FlipVehicle_TogglesTheFlippedFlag()
    {
        var context = CreateContext();
        var (consist, vehicles) = GetServiceConsist(context.ScenarioDocument);
        var target = vehicles[2];
        var request = new FlipConsistVehicleRequest
        {
            Consist = consist,
            Vehicle = target,
        };

        await new FlipConsistVehicle(request).Run(context);

        var flipped = GetVehicleElement(context.ScenarioDocument, target.Id).SelectTextContent("Flipped");
        var expected = target.Flipped ? "0" : "1";

        Assert.Equal(expected, flipped);
    }

    [Fact]
    public async Task MoveVehicle_ReordersVehiclesAndInitialNumbers()
    {
        var context = CreateContext();
        var (consist, vehicles) = GetServiceConsist(context.ScenarioDocument);
        var vehicleNumbers = vehicles.Select(vehicle => vehicle.UniqueNumber ?? string.Empty).ToList();

        SetInitialVehicleNumbers(context.ScenarioDocument, vehicleNumbers);

        var moved = vehicles[1];
        var request = new MoveConsistVehicleRequest
        {
            Consist = consist,
            Vehicle = moved,
            NewIndex = 3,
        };

        await new MoveConsistVehicle(request).Run(context);

        var (_, reordered) = GetServiceConsist(context.ScenarioDocument);
        var reorderedIds = reordered.Select(vehicle => vehicle.Id).ToList();
        var expectedIds = vehicles.Select(vehicle => vehicle.Id).ToList();

        expectedIds.RemoveAt(1);
        expectedIds.Insert(3, moved.Id);

        var reorderedNumbers = reordered.Select(vehicle => vehicle.UniqueNumber ?? string.Empty).ToList();
        var initialNumbers = GetInitialVehicleNumbers(context.ScenarioDocument);

        Assert.Equal(expectedIds, reorderedIds);
        Assert.Equal(reorderedNumbers, initialNumbers);
    }

    [Fact]
    public async Task MoveVehicle_LeavesInitialNumbersThatDoNotMatchTheVehicles()
    {
        var context = CreateContext();
        var (consist, vehicles) = GetServiceConsist(context.ScenarioDocument);
        var originalNumbers = GetInitialVehicleNumbers(context.ScenarioDocument);
        var request = new MoveConsistVehicleRequest
        {
            Consist = consist,
            Vehicle = vehicles[1],
            NewIndex = 3,
        };

        await new MoveConsistVehicle(request).Run(context);

        var initialNumbers = GetInitialVehicleNumbers(context.ScenarioDocument);

        Assert.Equal(originalNumbers, initialNumbers);
    }

    [Fact]
    public async Task MoveVehicle_ToItsOwnPosition_ChangesNothing()
    {
        var context = CreateContext();
        var (consist, vehicles) = GetServiceConsist(context.ScenarioDocument);
        var request = new MoveConsistVehicleRequest
        {
            Consist = consist,
            Vehicle = vehicles[2],
            NewIndex = 2,
        };

        await new MoveConsistVehicle(request).Run(context);

        var (_, unchanged) = GetServiceConsist(context.ScenarioDocument);
        var originalIds = vehicles.Select(vehicle => vehicle.Id);
        var unchangedIds = unchanged.Select(vehicle => vehicle.Id);

        Assert.Equal(originalIds, unchangedIds);
    }

    private static ConsistCommandContext CreateContext()
    {
        var document = ParseResource("Scenario.bin.xml");
        var route = new Route
        {
            Id = "route",
            Name = "Route",
            RoutePropertiesPath = "/routes/route/RouteProperties.xml",
            DirectoryPath = "/routes/route",
            PackagingType = PackagingType.Unpacked,
        };
        var scenario = new Scenario
        {
            Id = "scenario",
            Route = route,
            Name = "Scenario",
            Locomotive = "Locomotive",
            Duration = 60,
            Rating = 1,
            DirectoryPath = "/routes/route/Scenarios/scenario",
            ScenarioPropertiesPath = "/routes/route/Scenarios/scenario/ScenarioProperties.xml",
            AssetPath = AssetPath.Empty,
        };

        return new ConsistCommandContext
        {
            Scenario = scenario,
            ScenarioDocument = document,
            ScenarioPropertiesDocument = document,
        };
    }

    private static (Consist Consist, List<ConsistRailVehicle> Vehicles) GetServiceConsist(IDocument document)
    {
        var nodes = ScenarioNode.ReadConsists(document);
        var consist = nodes
            .Select((node, index) => Consist.ParseScenarioConsist(node, index))
            .OfType<Consist>()
            .First(candidate => candidate.Id == ServiceConsistId);
        var vehicles = Scenario.GetServiceConsistVehicles(nodes, consist);

        return (consist, vehicles);
    }

    private static IElement GetVehicleElement(IDocument document, string id)
    {
        return document.QuerySelectorAll("RailVehicles cOwnedEntity").First(element => element.GetAttribute("d:id") == id);
    }

    private static List<string> GetInitialVehicleNumbers(IDocument document)
    {
        var serviceConsist = document.QuerySelectorAll("cConsist").First(element => element.GetAttribute("d:id") == ServiceConsistId);

        return serviceConsist.QuerySelectorAll("Driver cDriver InitialRV e").Select(entry => entry.TextContent).ToList();
    }

    private static void SetInitialVehicleNumbers(IDocument document, List<string> numbers)
    {
        var serviceConsist = document.QuerySelectorAll("cConsist").First(element => element.GetAttribute("d:id") == ServiceConsistId);
        var entries = serviceConsist.QuerySelectorAll("Driver cDriver InitialRV e").ToList();

        foreach (var (entry, number) in entries.Zip(numbers))
        {
            entry.SetTextContent(number);
        }
    }

    private static IDocument ParseResource(string name)
    {
        using var resource = typeof(ConsistVehicleCommandTests).Assembly
            .GetManifestResourceStream($"RailworksForge.Core.Tests.Resources.{name}")!;

        return XmlParser.ParseDocument(resource);
    }
}
