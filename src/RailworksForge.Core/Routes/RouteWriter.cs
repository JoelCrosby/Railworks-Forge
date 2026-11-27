using AngleSharp.Dom;

using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Routes;

public static class RouteWriter
{
    // Written loose beside a packed route's archive, which the game prefers over the archived copy.
    public static async Task WritePropertiesDocument(Route route, IDocument document)
    {
        var destination = route.LoosePropertiesPath;
        var stagingPath = $"{destination}.forge-staging";

        await document.ToXmlAsync(stagingPath);

        File.Move(stagingPath, destination, overwrite: true);
    }
}
