using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands;

public class DeleteConsistVehicle : IConsistCommand
{
    private readonly DeleteConsistVehicleRequest _request;

    public DeleteConsistVehicle(DeleteConsistVehicleRequest request)
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

        var serviceConsist = document
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("Driver ServiceName Key", _request.Consist.ServiceId);

        if (serviceConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        var railVehicle = serviceConsist.QuerySelector($"RailVehicles cOwnedEntity[d:id='{_request.VehicleToDelete.Id}']");

        if (railVehicle is null)
        {
            throw new Exception("unable to find rail vehicle in scenario document");
        }

        railVehicle.Remove();

        if (railVehicle.Index() is 0)
        {

        }
    }

    private void GetUpdatedScenarioProperties(ConsistCommandContext context)
    {
        var document = context.ScenarioPropertiesDocument;
    }
}
