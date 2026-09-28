using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Xml;

using RailworksForge.Core;

namespace RailworksForge.UnitTests;

public class ScenarioDocumentCopyTests
{
    [Fact]
    public void DeepCopy_KeepsVehicleIdsAndMarkup()
    {
        using var resource = typeof(ScenarioDocumentCopyTests).Assembly
            .GetManifestResourceStream("RailworksForge.UnitTests.Resources.Scenario.bin.xml")!;
        var document = XmlParser.ParseDocument(resource);

        var copy = (IDocument)document.Clone(true);

        var formatter = new XmlMarkupFormatter { IsAlwaysSelfClosing = false };
        var originalIds = GetVehicleIds(document);

        Assert.NotEmpty(originalIds);
        Assert.Equal(originalIds, GetVehicleIds(copy));
        Assert.Equal(document.ToHtml(formatter), copy.ToHtml(formatter));
    }

    private static List<string?> GetVehicleIds(IDocument document)
    {
        return document
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .Select(element => element.GetAttribute("d:id"))
            .ToList();
    }
}
