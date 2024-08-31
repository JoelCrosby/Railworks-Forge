using System.Text;

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Xml;

using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Extensions;

public static class AngleSharpExtensions
{
    public static string SelectTextContent(this IElement node, string selector)
    {
        return node.QuerySelector(selector)?.Text().Trim().ReplaceLineEndings(string.Empty) ?? string.Empty;
    }

    public static string SelectTextContent(this IDocument document, string selector)
    {
        return document.DocumentElement.SelectTextContent(selector);
    }

    public static string SelectLocalisedStringContent(this IElement node, string selector)
    {
        var localisedSelector = $"{selector} Localisation-cUserLocalisedString";
        var localisationElement = node.QuerySelector(localisedSelector);

        if (localisationElement is null) return string.Empty;

        return localisationElement
            .Children
            .FirstOrDefault(child => string.IsNullOrWhiteSpace(child.TextContent) is false)?
            .Text()
            .Trim()
            .ReplaceLineEndings(string.Empty) ?? string.Empty;
    }

    public static string SelectLocalisedStringContent(this IDocument document, string selector)
    {
        return SelectLocalisedStringContent(document.DocumentElement, selector);
    }

    public static IElement? QueryByTextContent(this IHtmlCollection<IElement> elements, string selector, string key)
    {
        return elements.FirstOrDefault(el => el.SelectTextContent(selector) == key);
    }

    public static async Task ToXmlAsync(this IDocument document, string path)
    {
        var text = document.ToHtml(new XmlMarkupFormatter
        {
            IsAlwaysSelfClosing = false,
        });

        const string xmlTag = """<?xml version="1.0" encoding="utf-8"?>""";
        var output = new StringBuilder(xmlTag).Append(Environment.NewLine).Append(text).ToString();

        await File.WriteAllTextAsync(path, output);
    }

    public static void SetTextContent(this IElement element, string text)
    {
        var isEmpty = element.ChildNodes.Length is 0;
        var hasTextContent = element.ChildNodes.Length is 1 && element.FirstChild?.NodeType is NodeType.Text;

        if (isEmpty || hasTextContent)
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

    public static IElement CreateXmlElement(this IDocument document, string selector)
    {
        if (document is not Document doc)
        {
            throw new Exception("cannot create xml element, invalid document");
        }

        return new HtmlElement(doc, selector);
    }

    public static void UpdateBlueprintSetCollection(this IDocument document, Blueprint blueprint, string tagSelector)
    {
        if (document.QuerySelector(tagSelector) is not {} collectionElement)
        {
            return;
        }

        var needle = $"{blueprint.BlueprintSetIdProvider}:{blueprint.BlueprintSetIdProduct}".ToLowerInvariant();
        var providerProductSet = collectionElement
            .QuerySelectorAll("iBlueprintLibrary-cBlueprintSetID")
            .Aggregate(new Dictionary<string, int>(), (acc, curr) =>
            {
                var provider = curr.SelectTextContent("Provider");
                var product = curr.SelectTextContent("Product");
                var index = $"{provider}:{product}".ToLowerInvariant();

                var textIdValue = curr.GetAttribute("d:id");
                var isIntId = int.TryParse(textIdValue, out var idValue);
                var id = isIntId ? idValue : -1;

                acc.TryAdd(index, id);

                return acc;
            });

        if (providerProductSet.ContainsKey(needle))
        {
            return;
        }

        var currentEntryId = providerProductSet.OrderByDescending(pair => pair.Value).Select(p => p.Value).FirstOrDefault();
        var initialId = currentEntryId == 0 ? new Random().Next(10000, 99999) : currentEntryId;
        var entryId = initialId + 2;

        var setElement = CreateBlueprintSetElement(document, blueprint, entryId);

        collectionElement.AppendChild(setElement);
    }

    private static IElement CreateBlueprintSetElement(this IDocument document, Blueprint blueprint, int entryId)
    {
        var setElement = document.CreateXmlElement("iBlueprintLibrary-cBlueprintSetID");
        setElement.SetAttribute(Utilities.NS, "d:id", entryId.ToString());

        var providerElement = document.CreateXmlElement("Provider");
        providerElement.SetAttribute(Utilities.NS, "d:type", "cDeltaString");
        providerElement.SetTextContent(blueprint.BlueprintSetIdProvider);

        var productElement = document.CreateXmlElement("Product");
        productElement.SetAttribute(Utilities.NS, "d:type", "cDeltaString");
        productElement.SetTextContent(blueprint.BlueprintSetIdProduct);

        setElement.AppendChild(providerElement);
        setElement.AppendChild(productElement);

        return setElement;
    }
}
