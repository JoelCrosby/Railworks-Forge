using AngleSharp.Dom;

namespace RailworksForge.Core.Extensions;

public static class AngleHtmlExtensions
{
    public static string SelectTextContnet(this IParentNode node, string selector)
    {
        return node.QuerySelector(selector)?.TextContent ?? string.Empty;
    }
}
