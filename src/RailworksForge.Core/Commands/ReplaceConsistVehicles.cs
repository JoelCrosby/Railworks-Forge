using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core.Commands;

public class ReplaceConsistVehicles : IConsistCommand
{
    private readonly ReplaceVehiclesRequest _request;

    public ReplaceConsistVehicles(ReplaceVehiclesRequest request)
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

        foreach (var replacement in _request.Replacements)
        {
            var railVehicle = serviceConsist
                .QuerySelectorAll("RailVehicles cOwnedEntity")
                .FirstOrDefault(el => el.GetAttribute("d:id") == replacement.Target.Id);

            if (railVehicle is null)
            {
                throw new Exception("unable to find rail vehicle in scenario document");
            }

            var blueprint = replacement.Replacement.Blueprint;
            var flipped = replacement.Target.Flipped;
            var isLeadVehicle = railVehicle.ParentElement?.FirstElementChild == railVehicle;
            var previousNumber = railVehicle.SelectTextContent("UniqueNumber");

            var vehicleDocument = await blueprint.GetXmlDocument();
            var scenarioConsist = ScenarioConsist.ParseConsist(vehicleDocument, blueprint);
            var generated = await VehicleGenerator.GenerateVehicle(
                document,
                railVehicle,
                scenarioConsist,
                blueprint,
                flipped,
                previousNumber);

            railVehicle.Replace(generated.Element);

            var numberChanged = !string.IsNullOrEmpty(previousNumber) && previousNumber != generated.Number;

            if (numberChanged)
            {
                ServiceVehicleUpdates.RenameInitialVehicleNumber(serviceConsist, previousNumber, generated.Number);
                ServiceVehicleUpdates.RenameInstructionTargets(document, previousNumber, generated.Number);
            }

            ServiceVehicleUpdates.AddRequiredBlueprintSet(context.ScenarioPropertiesDocument, blueprint);

            if (isLeadVehicle)
            {
                ServiceVehicleUpdates.UpdateLeadVehicle(context.ScenarioPropertiesDocument, _request.Consist, replacement.Replacement);
            }
        }
    }
}
