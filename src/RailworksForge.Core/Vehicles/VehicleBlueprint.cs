using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Vehicles;

public class VehicleBlueprint
{
    public BlueprintType BlueprintType { get; private init; }

    public required Blueprint Blueprint { get; init; }

    public required ScenarioNode Element { get; init; }

    public static VehicleBlueprint Parse(ScenarioNode el)
    {
        var blueprintElement = el.QuerySelector("BlueprintID iBlueprintLibrary-cAbsoluteBlueprintID")
            ?? throw new InvalidDataException("Rail vehicle is missing its blueprint ID.");

        var blueprint = Blueprint.Parse(blueprintElement);
        var blueprintType = Utilities.ParseBlueprintType(el.QuerySelector("Component")?.FirstChild?.Name);

        return new VehicleBlueprint
        {
            Blueprint = blueprint,
            BlueprintType = blueprintType,
            Element = el,
        };
    }
}
