using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands;

public class DeleteConsist : IConsistCommand
{
    private readonly TargetConsist _target;

    public DeleteConsist(TargetConsist target)
    {
        _target = target;
    }

    public Task Run(ConsistCommandContext context)
    {
        GetDeleteUpdatedScenario(context);
        GetDeleteScenarioProperties(context);

        return Task.CompletedTask;
    }

    private void GetDeleteUpdatedScenario(ConsistCommandContext context)
    {
        var document = context.ScenarioDocument;

        foreach (var consist in _target.GetConsists())
        {
            var consistElement = Consist.GetServiceConsist(document, consist);

            if (consistElement is null)
            {
                throw new Exception("unable to find scenario consist");
            }

            consistElement.RemoveFromParent();
        }
    }

    private void GetDeleteScenarioProperties(ConsistCommandContext context)
    {
        var document = context.ScenarioPropertiesDocument;

        foreach (var consist in _target.GetConsists())
        {
            var element = document
                .QuerySelectorAll("sDriverFrontEndDetails")
                .QueryByTextContent("ServiceName Key", consist.ServiceId);

            element?.RemoveFromParent();
        }
    }
}
