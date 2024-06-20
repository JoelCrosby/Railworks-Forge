using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public class Consist : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? LocoAuthor { get; init; }

    public LocoClass? LocoClass { get; set; }

    public required string ServiceName { get; init; }

    public bool PlayerDriver { get; init; }

    public required string ServiceId { get; init; }
}

public class ConsistRailVehicle : Blueprint
{
    public required string Id { get; init; }

    public required string LocomotiveName { get; init; }

    public string? UniqueNumber { get; init; }

    public bool Flipped { get; init; }
}

public class ConsistBlueprint
{
    public required string LocomotiveName { get; init; }

    public required string DisplayName { get; init; }

    public LocoClass EngineType { get; init; }

    public string? EraStartYear { get; init; }

    public string? EraEndYear { get; init; }

    public required List<ConsistEntry> ConsistEntries { get; init; }

    public static ConsistBlueprint Parse(IElement el)
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

        return new ConsistBlueprint
        {
            LocomotiveName = locomotiveName,
            DisplayName = displayName,
            EngineType = LocoClassUtils.Parse(engineType),
            EraStartYear = eraStartYear,
            EraEndYear = eraEndYear,
            ConsistEntries = entries,
        };
    }
}

public class ConsistEntry
{
    public required string BlueprintId { get; init; }

    public required string BlueprintIdProduct { get; init; }

    public required string BlueprintIdProvider { get; init; }

    public required bool Flipped { get; init; }
}

public enum AcquisitionState
{
    Unknown = 0,
    Found = 1,
    Missing = 2,
}
