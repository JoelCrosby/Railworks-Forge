using AngleSharp.Dom;

namespace RailworksForge.Core.Models.Common;

public class VehicleBlueprint
{
    public BlueprintType BlueprintType { get; private init; }

    public Blueprint Blueprint { get; private init; }

    public IElement Element { get; private init; }

    public static VehicleBlueprint Parse(IElement el)
    {
        var blueprint = Blueprint.Parse(el.QuerySelector("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID"));
        var blueprintType = Utilities.ParseBlueprintType(el.QuerySelector("Component")?.FirstElementChild?.NodeName);

        return new VehicleBlueprint
        {
            Blueprint = blueprint,
            BlueprintType = blueprintType,
            Element = el,
        };
    }
}
