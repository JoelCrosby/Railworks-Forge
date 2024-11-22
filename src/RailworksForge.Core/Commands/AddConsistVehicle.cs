using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands;

public class AddConsistVehicle : IConsistCommand
{
    private readonly AddConsistVehicleRequest _request;

    public AddConsistVehicle(AddConsistVehicleRequest request)
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
