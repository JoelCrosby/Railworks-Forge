using RailworksForge.Core.Commands;
using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.models;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistEditService
{
    public Task AddVehicle(Scenario scenario, RollingStockEntry vehicle)
    {
        var request = new AddConsistVehicleRequest
        {
            VehicleToAdd = vehicle,
        };

        return Run(scenario, new AddConsistVehicle(request));
    }

    public Task ReplaceVehicles(
        Scenario scenario,
        Consist consist,
        IEnumerable<ConsistRailVehicle> targets,
        RollingStockEntry replacement)
    {
        var replacements = targets
            .Select(target => new VehicleReplacement
            {
                Replacement = replacement,
                Target = target,
            })
            .ToList();

        var request = new ReplaceVehiclesRequest
        {
            Consist = consist,
            Replacements = replacements,
        };

        return Run(scenario, new ReplaceConsistVehicles(request));
    }

    public Task DeleteVehicle(Scenario scenario, Consist consist, ConsistRailVehicle vehicle)
    {
        var request = new DeleteConsistVehicleRequest
        {
            Consist = consist,
            VehicleToDelete = vehicle,
        };

        return Run(scenario, new DeleteConsistVehicle(request));
    }

    public Task ReplaceConsists(Scenario scenario, IEnumerable<Consist> targets, PreloadConsist replacement)
    {
        var request = new ReplaceConsistRequest
        {
            Target = new TargetConsist(targets),
            PreloadConsist = replacement,
        };

        return Run(scenario, new ReplaceConsist(request));
    }

    public Task DeleteConsists(Scenario scenario, IEnumerable<Consist> targets)
    {
        var target = new TargetConsist(targets);

        return Run(scenario, new DeleteConsist(target));
    }

    private static Task Run(Scenario scenario, IConsistCommand command)
    {
        var runner = new ConsistCommandRunner
        {
            Scenario = scenario,
            Commands = [command],
        };

        return runner.Run();
    }
}
