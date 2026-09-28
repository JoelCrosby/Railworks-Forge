using RailworksForge.Core.Commands;
using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.models;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistEditService
{
    public Task<ConsistEditSession> BeginSession(Scenario scenario, Consist consist, CancellationToken cancellationToken)
    {
        return ConsistEditSession.Begin(scenario, consist, cancellationToken);
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
