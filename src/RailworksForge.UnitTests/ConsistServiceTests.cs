using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.UnitTests;

public class ConsistServiceTests
{
    [Fact]
    public async Task ReplaceConsistsReturnsCorrectly()
    {
        var scenario = DesignData.DesignData.Scenario;
        var targetConsist = DesignData.DesignData.Scenario.Consists.First();

        var consist = new PreloadConsist
        {
            Blueprint = new Blueprint
            {
                BlueprintSetIdProduct = "WCML-South",
                BlueprintSetIdProvider = "DTG",
                BlueprintId = @"RailVehicles\Electric\Class390\Default\DMSO\Class390DMSO.xml",
            },
            LocomotiveName = "Class 390 'Pendolino'",
            DisplayName = "Class 390 'Pendolino' Avanti - 9 Car Set",
            EraStartYear = "2002",
            EraEndYear = "2050",
            EngineType = LocoClass.Electric,
            IsReskin = false,
            ConsistEntries =
            [
                new ()
                {
                    Flipped = false,
                    Blueprint = new Blueprint
                    {
                        BlueprintId = @"RailVehicles\Electric\Class390\Default\DMSO\Class390DMSO.xml",
                        BlueprintSetIdProvider = "DTG",
                        BlueprintSetIdProduct = "WCML-South",
                    }
                },
                new ()
                {
                    Flipped = true,
                    Blueprint = new Blueprint
                    {
                        BlueprintId = @"RailVehicles\Electric\Class390\Default\MS\MS_b.xml",
                        BlueprintSetIdProvider = "DTG",
                        BlueprintSetIdProduct = "WCML-South",
                    }
                },
                new ()
                {
                    Flipped = true,
                    Blueprint = new Blueprint
                    {
                        BlueprintId = @"RailVehicles\Electric\Class390\Default\PTSRMB\PTSRMB.xml",
                        BlueprintSetIdProvider = "DTG",
                        BlueprintSetIdProduct = "WCML-South",
                    }
                },
            ],
        };

        var target = new TargetConsist(targetConsist);

        await ConsistService.ReplaceConsist(target, consist, scenario);
    }
}
