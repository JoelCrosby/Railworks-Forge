using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands;

public class ReplaceConsistVehicles : IConsistCommand
{
    private readonly ReplaceVehiclesRequest _request;

    public ReplaceConsistVehicles(ReplaceVehiclesRequest request)
    {
        _request = request;
    }

    public Task Run(ConsistCommandContext context)
    {
        GetUpdatedScenario(context);
        GetUpdatedScenarioProperties(context);

        return Task.CompletedTask;
    }

    private void GetUpdatedScenario(ConsistCommandContext context)
    {
        var document = context.ScenarioDocument;
    }

    private void GetUpdatedScenarioProperties(ConsistCommandContext context)
    {
        var document = context.ScenarioPropertiesDocument;
    }
}
