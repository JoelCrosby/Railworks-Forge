using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public class RollingStockEntry
{
    public required Blueprint Blueprint { get; init; }

    public required string LocomotiveName { get; init; }

    public required string DisplayName { get; init; }

    public required BlueprintType BlueprintType { get; init; }

    public static RollingStockEntry Parse(IElement el)
    {
        var blueprint = ParseBlueprint(el);
        var locomotiveName = el.SelectTextContent("Name");
        var displayName = el.SelectLocalisedStringContent("DisplayName");
        var blueprintType = Utilities.ParseBlueprintType(el.FirstElementChild?.NodeName);

        return new RollingStockEntry
        {
            LocomotiveName = locomotiveName,
            DisplayName = displayName,
            Blueprint = blueprint,
            BlueprintType = blueprintType,
        };
    }

    private static Blueprint ParseBlueprint(IElement el)
    {
        return new Blueprint
        {
            BlueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Provider"),
            BlueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Product"),
            BlueprintId = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID"),
        };
    }
}
