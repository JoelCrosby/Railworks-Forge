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

    public static PreloadConsist Parse(IElement el)
    {
        var locomotiveName = el.SelectTextContnet("LocoName English");
        var displayName = el.SelectTextContnet("DisplayName English");
        var engineType = el.SelectTextContnet("EngineType");
        var eraStartYear = el.SelectTextContnet("EraStartYear");
        var eraEndYear = el.SelectTextContnet("EraEndYear");

        var entries = el
            .QuerySelectorAll("cConsistEntry")
            .Select(entry => new ConsistEntry
            {
                BlueprintIdProvider = entry.SelectTextContnet("Provider"),
                BlueprintIdProduct = entry.SelectTextContnet("Product"),
                BlueprintId = entry.SelectTextContnet("BlueprintID"),
                Flipped = entry.SelectTextContnet("Flipped") is "eTrue",
            })
            .ToList();

        return new PreloadConsist
        {
            BlueprintId = entries.First().BlueprintId,
            LocomotiveName = locomotiveName,
            DisplayName = displayName,
            EngineType = LocoClassUtils.Parse(engineType),
            EraStartYear = eraStartYear,
            EraEndYear = eraEndYear,
            ConsistEntries = entries,
        };
    }
}
