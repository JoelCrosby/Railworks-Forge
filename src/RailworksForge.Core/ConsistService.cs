using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistService
{
    public static async Task ReplaceConsist(Consist target, ConsistBlueprint blueprint, Scenario scenario)
    {
        var scenarioDocument = await scenario.GetXmlDocument();

        var targetElement = scenarioDocument.QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("Driver ServiceName Key") == target.ServiceId);
    }
}
