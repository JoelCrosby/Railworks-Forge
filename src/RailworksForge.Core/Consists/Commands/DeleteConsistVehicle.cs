using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Consists.Commands;

public class DeleteConsistVehicle : IConsistCommand
{
    private readonly DeleteConsistVehicleRequest _request;

    public DeleteConsistVehicle(DeleteConsistVehicleRequest request)
    {
        _request = request;
    }

    public async Task Run(ConsistCommandContext context)
    {
        var document = context.ScenarioDocument;

        var serviceConsist = Consist.GetServiceConsist(document, _request.Consist);

        if (serviceConsist is null)
        {
            throw new Exception("unable to find scenario consist");
        }

        var railVehicle = serviceConsist
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .FirstOrDefault(el => el.GetAttribute("d:id") == _request.VehicleToDelete.Id);

        if (railVehicle is null)
        {
            throw new Exception("unable to find rail vehicle in scenario document");
        }

        var wasLeadVehicle = railVehicle.ParentElement?.FirstElementChild == railVehicle;
        var number = railVehicle.SelectTextContent("UniqueNumber");

        railVehicle.Remove();

        if (!string.IsNullOrEmpty(number))
        {
            ServiceVehicleUpdates.RemoveInitialVehicleNumber(serviceConsist, number);
        }

        if (!wasLeadVehicle)
        {
            return;
        }

        var entry = serviceConsist.QuerySelector("RailVehicles")?.FirstElementChild;

        if (entry is null)
        {
            return;
        }

        await ServiceVehicleUpdates.UpdateLeadVehicleFrom(context.ScenarioPropertiesDocument, _request.Consist, entry);
    }
}
