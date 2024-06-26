using AngleSharp.Dom;

using RailworksForge.Core.Extensions;

namespace RailworksForge.Core.Models;

public class PreloadConsist
{
    public required string LocomotiveName { get; init; }

    public required string DisplayName { get; init; }

    public LocoClass EngineType { get; init; }

    public string? EraStartYear { get; init; }

    public string? EraEndYear { get; init; }

    public required List<ConsistEntry> ConsistEntries { get; init; }

    public required string BlueprintId { get; init; }

    public required string BlueprintIdProvider { get; init; }

    public required string BlueprintIdProduct { get; init; }

    public static PreloadConsist Parse(IElement el)
    {
        var locomotiveName = el.SelectTextContent("LocoName English");
        var displayName = el.SelectTextContent("DisplayName English");
        var engineType = el.SelectTextContent("EngineType");
        var eraStartYear = el.SelectTextContent("EraStartYear");
        var eraEndYear = el.SelectTextContent("EraEndYear");

        var entries = el
            .QuerySelectorAll("cConsistEntry")
            .Select(entry => new ConsistEntry
            {
                BlueprintIdProvider = entry.SelectTextContent("Provider"),
                BlueprintIdProduct = entry.SelectTextContent("Product"),
                BlueprintId = entry.SelectTextContent("BlueprintID"),
                Flipped = entry.SelectTextContent("Flipped") is "eTrue",
            })
            .ToList();

        var locomotive = entries.First();

        return new PreloadConsist
        {
            BlueprintId = locomotive.BlueprintId,
            BlueprintIdProduct = locomotive.BlueprintIdProduct,
            BlueprintIdProvider = locomotive.BlueprintIdProvider,
            LocomotiveName = locomotiveName,
            DisplayName = displayName,
            EngineType = LocoClassUtils.Parse(engineType),
            EraStartYear = eraStartYear,
            EraEndYear = eraEndYear,
            ConsistEntries = entries,
        };
    }
}
