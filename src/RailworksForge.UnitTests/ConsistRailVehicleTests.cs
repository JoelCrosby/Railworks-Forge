using RailworksForge.Core.Models;

namespace RailworksForge.UnitTests;

public class ConsistRailVehicleTests
{
    [Fact]
    public void Vehicles_WithTheSameBlueprint_AreDistinct()
    {
        var first = CreateWagon("1", index: 1, uniqueNumber: "5020123");
        var second = CreateWagon("2", index: 2, uniqueNumber: "5020102");
        List<ConsistRailVehicle> vehicles = [first, second];

        Assert.NotEqual(first, second);
        Assert.Equal(1, vehicles.IndexOf(second));
        Assert.Equal(2, vehicles.ToHashSet().Count);
    }

    private static ConsistRailVehicle CreateWagon(string id, int index, string uniqueNumber)
    {
        return new ConsistRailVehicle
        {
            Id = id,
            Index = index,
            EntityID = id,
            LocomotiveName = "Box MJA (Green)(Empty)",
            UniqueNumber = uniqueNumber,
            BlueprintSetIdProvider = "Oovee",
            BlueprintSetIdProduct = "MJAWagonPack01",
            BlueprintId = @"railvehicles\freight\mja\default\mja_empty.xml",
            SearchIndex = string.Empty,
        };
    }
}
