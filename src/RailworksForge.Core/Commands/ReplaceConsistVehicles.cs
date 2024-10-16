using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

using Serilog;

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
        await GetUpdatedScenario(context);

        GetUpdatedScenarioProperties(context);
    }

    private async Task GetUpdatedScenario(ConsistCommandContext context)
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

            var vehicleDocument = await blueprint.GetXmlDocument();

            var scenarioConsist = ScenarioConsist.ParseConsist(vehicleDocument, blueprint);
            var replacementEl = await VehicleGenerator.GenerateVehicle(document, railVehicle, scenarioConsist, blueprint, flipped);

            railVehicle.Replace(replacementEl.Element);
        }
    }

    private void GetUpdatedScenarioProperties(ConsistCommandContext context)
    {
        var document = context.ScenarioPropertiesDocument;
        var consist = _request.Consist;

        var serviceElement = document
            .QuerySelectorAll("sDriverFrontEndDetails")
            .QueryByTextContent("ServiceName Key", _request.Consist.ServiceId);

        if (serviceElement is null)
        {
            Log.Warning("Could not find service {Service} in scenario properties file", consist.ServiceName);
            return;
        }
    }
}
