using AngleSharp.Dom;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Core.Models;

public record RailVehicle
{
    public required string LocoName { get; init; }

    public required string DisplayName { get; init; }

    public required Blueprint Blueprint { get; init; }

    public virtual bool Equals(RailVehicle? other)
    {
        if (other is null) return false;

        return LocoName == other.LocoName;
    }

    public override int GetHashCode()
    {
        return LocoName.GetHashCode();
    }

    public static RailVehicle Parse(IElement el)
    {
        var locomotiveName = el.SelectLocalisedStringContent("LocoName");
        var displayName = el.SelectLocalisedStringContent("DisplayName");

        var blueprint = new Blueprint
        {
            BlueprintSetIdProvider = el.SelectTextContent("Provider"),
            BlueprintSetIdProduct = el.SelectTextContent("Product"),
            BlueprintId = el.SelectTextContent("BlueprintID"),
        };

        return new RailVehicle
        {
            Blueprint = blueprint,
            DisplayName = displayName,
            LocoName = locomotiveName,
        };
    }
}
