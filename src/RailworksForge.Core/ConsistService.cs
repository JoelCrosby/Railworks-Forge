using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class ConsistService
{
    public static async Task ReplaceConsist(Consist target, SavedConsist consist, Scenario scenario)
    {
        var scenarioDocument = await scenario.GetXmlDocument();
        var consistDocument = await GetConsistDocument(consist);

        var targetElement = scenarioDocument.QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("Driver ServiceName Key") == target.ServiceId);
    }

    private static async Task<IHtmlDocument> GetConsistDocument(SavedConsist consist)
    {
        return await new HtmlParser().ParseDocumentAsync(consist.ConsistElement);
    }
}
