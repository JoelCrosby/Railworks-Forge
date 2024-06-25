using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Xml;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Exceptions;

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

    public static async Task ToXmlAsync(this IXmlDocument document, string path)
    {
        var text = document.ToHtml(new XmlMarkupFormatter
        {
            IsAlwaysSelfClosing = false,
        });

        await File.WriteAllTextAsync(path, text);
    }

    public static void SetTextContent(this IElement element, string text)
    {
        if (element.ChildNodes.Length is 1 && element.FirstChild?.NodeType is NodeType.Text)
        {
            element.TextContent = text;
        }
        else
        {
            XmlException.ThrowInvalidNode(element, "failed to set text content as node does not contain only text");
        }
    }

    public static void UpdateTextElement(this IElement element, string selector, string text)
    {
        if (element.QuerySelector(selector) is {} selectedElement)
        {
            selectedElement.SetTextContent(text);
        }
    }
}
