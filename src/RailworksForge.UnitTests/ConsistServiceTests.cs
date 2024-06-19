using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Examples;

namespace RailworksForge.UnitTests;

public class ConsistServiceTests
{
    [Fact]
    public async Task ReplaceConsistsReturnsCorrectly()
    {
        var scenario = Example.Scenario;

        var targetConsist = Example.Scenario.Consists.ElementAt(1);

        var consist = new SavedConsist
        {
            Name = "Test Consist",
            LocomotiveName = "Class 390 'Pendolino'",
            ConsistElement = string.Empty,
        };

        await ConsistService.ReplaceConsist(targetConsist, consist, scenario);
    }
}
