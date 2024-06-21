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
        var targetConsist = Example.Scenario.Consists.First();

        var consist = new PreloadConsist
        {
            LocomotiveName = "Class 390 'Pendolino'",
            DisplayName = "Class 390 'Pendolino' Avanti - 9 Car Set",
            EraStartYear = "2002",
            EraEndYear = "2050",
            EngineType = LocoClass.Electric,
            ConsistEntries =
            [
                new ()
                {
                    BlueprintId = @"RailVehicles\Electric\Class390\Default\DMSO\Class390DMSO.xml",
                    BlueprintIdProvider = "DTG",
                    BlueprintIdProduct = "WCML-South",
                    Flipped = false,
                },
                new ()
                {
                    BlueprintId = @"RailVehicles\Electric\Class390\Default\MS\MS_b.xml",
                    BlueprintIdProvider = "DTG",
                    BlueprintIdProduct = "WCML-South",
                    Flipped = true,
                },
                new ()
                {
                    BlueprintId = @"RailVehicles\Electric\Class390\Default\PTSRMB\PTSRMB.xml",
                    BlueprintIdProvider = "DTG",
                    BlueprintIdProduct = "WCML-South",
                    Flipped = true,
                },
            ],
        };

        await ConsistService.ReplaceConsist(targetConsist, consist, scenario);
    }
}
