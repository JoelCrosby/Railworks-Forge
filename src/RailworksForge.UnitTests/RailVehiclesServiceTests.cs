using RailworksForge.Core;

namespace RailworksForge.UnitTests;

public class RailVehiclesServiceTests
{
    [Fact]
    public async Task GetRailVehicles_ReturnsRailVehicles()
    {
        var results = await new RailVehiclesService().GetRailVehicles();

        Assert.NotEmpty(results);
    }
}
