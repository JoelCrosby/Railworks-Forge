using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public class PreloadConsist
{
    public required string LocomotiveName { get; init; }

    public required string DisplayName { get; init; }

    public LocoClass EngineType { get; init; }

    public string? EraStartYear { get; init; }

    public string? EraEndYear { get; init; }

    public required List<ConsistEntry> ConsistEntries { get; init; }

    public required Blueprint Blueprint { get; init; }

    public required bool IsReskin { get; init; }

    public static PreloadConsist Parse(IElement el)
    {
        var locomotiveName = el.SelectTextContent("LocoName English");
        var displayName = el.SelectTextContent("DisplayName English");
        var engineType = el.SelectTextContent("EngineType");
        var eraStartYear = el.SelectTextContent("EraStartYear");
        var eraEndYear = el.SelectTextContent("EraEndYear");
        var isReskin = el.FirstElementChild?.NodeName == "cReskinBlueprint";

        var entries = el
            .QuerySelectorAll("cConsistEntry")
            .Select(entry =>  new ConsistEntry
            {
                Flipped = entry.SelectTextContent("Flipped") is "eTrue",
                Blueprint = new Blueprint
                {
                    BlueprintSetIdProvider = entry.SelectTextContent("Provider"),
                    BlueprintSetIdProduct = entry.SelectTextContent("Product"),
                    BlueprintId = entry.SelectTextContent("BlueprintID"),
                },
            })
            .ToList();

        var locomotive = entries.First();

        var blueprint = new Blueprint
        {
            BlueprintId = locomotive.Blueprint.BlueprintId,
            BlueprintSetIdProduct = locomotive.Blueprint.BlueprintSetIdProduct,
            BlueprintSetIdProvider = locomotive.Blueprint.BlueprintSetIdProvider,
        };

        return new PreloadConsist
        {
            IsReskin = isReskin,
            Blueprint = blueprint,
            LocomotiveName = locomotiveName,
            DisplayName = displayName,
            EngineType = LocoClassUtils.Parse(engineType),
            EraStartYear = eraStartYear,
            EraEndYear = eraEndYear,
            ConsistEntries = entries,
        };
    }


}
