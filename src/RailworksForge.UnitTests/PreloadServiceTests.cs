using RailworksForge.Core;

namespace RailworksForge.UnitTests;

public class PreloadServiceTests
{
    [Fact]
    public async Task GetRailVehicles_ReturnsRailVehicles()
    {
        var results = await PreloadService.GetRailVehicles();

        Assert.NotEmpty(results);
    }
}
