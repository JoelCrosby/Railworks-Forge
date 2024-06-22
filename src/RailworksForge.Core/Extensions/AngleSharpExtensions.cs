using AngleSharp.Dom;
using AngleSharp.Xml;
using AngleSharp.Xml.Dom;

namespace RailworksForge.Core.Extensions;

public static class AngleSharpExtensions
{
    public static string SelectTextContent(this IParentNode node, string selector)
    {
        return node.QuerySelector(selector)?.TextContent ?? string.Empty;
    }

    public static IElement? QueryByTextContent(this IHtmlCollection<IElement> elements, string selector, string key)
    {
        return elements.FirstOrDefault(el => el.SelectTextContent(selector) == key);
    }

    public static async Task ToXmlAsync(this IXmlDocument document, Stream stream)
    {
        var text = document.ToXml();

        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text);
    }
}
