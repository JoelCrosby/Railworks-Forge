using AngleSharp.Xml.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core;

public class TrackService
{
    public static async Task ReplaceTracks(Route route, ReplaceTracksRequest request)
    {
        var document = await route.GetTrackDocument();
        var propertiesDocument = await route.GetRoutePropertiesDocument();

        if (document is null)
        {
            throw new Exception("failed to read route tracks document");
        }

        if (propertiesDocument is null)
        {
            throw new Exception("failed to read route properties document");
        }

        var updatedDocument = UpdateTracks(document, request);

        await WriteTracksDocument(updatedDocument, route);
    }

    private static IXmlDocument UpdateTracks(IXmlDocument document, ReplaceTracksRequest request)
    {
        var blueprints = document.QuerySelectorAll("Network-cSectionGenericProperties BlueprintID").ToList();

        var replacementMap = request.Replacements
            .Where(r => r.ReplacementBlueprint is not null)
            .ToDictionary(k => k.Blueprint, v => v.ReplacementBlueprint);

        foreach(var element in blueprints)
        {
            var provider = element.SelectTextContent("Provider");
            var product = element.SelectTextContent("Product");
            var blueprintId = element.SelectTextContent("BlueprintID");

            var blueprint = new Blueprint
            {
                BlueprintId = blueprintId,
                BlueprintSetIdProduct = product,
                BlueprintSetIdProvider = provider,
            };

            if (replacementMap.TryGetValue(blueprint, out var replacement) is false)
            {
                continue;
            }

            if (replacement is null)
            {
                continue;
            }

            element.UpdateTextElement("Provider", replacement.BlueprintSetIdProvider);
            element.UpdateTextElement("Product", replacement.BlueprintSetIdProduct);
            element.UpdateTextElement("BlueprintID", replacement.BlueprintId);
        }

        return document;
    }

    private static async Task WriteTracksDocument(IXmlDocument document, Route route)
    {
        var destination = Path.Join(Paths.GetHomeDirectory(), "Downloads", "Tracks.bin.xml");

        await document.ToXmlAsync(destination);
        await Serz.Convert(destination, true);
    }
}
