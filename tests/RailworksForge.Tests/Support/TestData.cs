using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Packaging;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;
using RailworksForge.Core.Vehicles;

namespace RailworksForge.Tests.Support;

public static class TestData
{
    public static Route Route(string name = "West Coast Main Line")
    {
        return new Route
        {
            Id = name,
            Name = name,
            RoutePropertiesPath = $"/routes/{name}/RouteProperties.xml",
            DirectoryPath = $"/routes/{name}",
            PackagingType = PackagingType.Unpacked,
        };
    }

    public static Scenario Scenario(string name = "1A01 Euston - Glasgow")
    {
        return DesignData.DesignData.Scenario with
        {
            Id = name,
            Name = name,
            SearchIndex = name.ToLowerInvariant(),
        };
    }

    public static ScenarioPlayerInfo PlayerInfo(int score)
    {
        return new ScenarioPlayerInfo
        {
            ScenarioId = "scenario",
            Score = score,
            Completion = "Success",
            MedalsAwarded = 1,
        };
    }

    public static Blueprint Blueprint(string blueprintId = @"RailVehicles\Class390.xml")
    {
        return new Blueprint
        {
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "WCML",
            BlueprintId = blueprintId,
        };
    }

    public static Consist Consist(
        string serviceName = "1A01",
        AcquisitionState state = AcquisitionState.Found,
        bool isPlayer = false)
    {
        return new Consist
        {
            Id = serviceName,
            LocomotiveName = "Class 390",
            ServiceName = serviceName,
            ServiceId = serviceName,
            SearchIndex = serviceName.ToLowerInvariant(),
            Vehicles = [],
            ConsistAcquisitionState = state,
            PlayerDriver = isPlayer,
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "WCML",
            BlueprintId = @"RailVehicles\Class390.xml",
        };
    }

    public static ConsistRailVehicle RailVehicle(string id = "1")
    {
        return new ConsistRailVehicle
        {
            Id = id,
            LocomotiveName = "Class 390",
            EntityID = id,
            SearchIndex = id,
            BlueprintSetIdProvider = "DTG",
            BlueprintSetIdProduct = "WCML",
            BlueprintId = @"RailVehicles\Class390\DMRF.xml",
        };
    }

    public static RollingStockEntry RollingStock(string name = "Class 390 DMRF")
    {
        return new RollingStockEntry
        {
            Blueprint = Blueprint($@"RailVehicles\{name}.xml"),
            LocomotiveName = name,
            DisplayName = name,
            BlueprintType = BlueprintType.Engine,
        };
    }

    public static PreloadConsist PreloadConsist(string name = "Class 390 9-car")
    {
        return new PreloadConsist
        {
            LocomotiveName = name,
            DisplayName = name,
            ConsistEntries = [],
            Blueprint = Blueprint($@"PreLoad\{name}.xml"),
            IsReskin = false,
        };
    }

    public static BrowserDirectory ProductDirectory(string name = "WCML")
    {
        var product = new ProductDirectory
        {
            Name = name,
            Path = $"/assets/DTG/{name}",
            ContainsRailVehicles = true,
            ContainsPreloadData = true,
        };

        return new BrowserDirectory(product, _ => true);
    }

    public static BrowserDirectory ProviderDirectory(string name = "DTG")
    {
        var provider = new ProviderDirectory
        {
            Name = name,
            Path = $"/assets/{name}",
            Products = [],
        };

        return new BrowserDirectory(provider, _ => true);
    }

    public static Track Track(string name = "Concrete Sleepers")
    {
        return new Track
        {
            Name = name,
            Blueprint = Blueprint($@"RailNetwork\Track\{name}.xml"),
        };
    }

    public static VehicleSearchResult SearchResult(int total, params RollingStockEntry[] vehicles)
    {
        return new VehicleSearchResult([.. vehicles], total);
    }
}
