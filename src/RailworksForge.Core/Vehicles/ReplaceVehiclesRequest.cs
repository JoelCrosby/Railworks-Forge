using RailworksForge.Core.Consists;

namespace RailworksForge.Core.Vehicles;

public record ReplaceVehiclesRequest
{
    public required List<VehicleReplacement> Replacements { get; init; }

    public required Consist Consist { get; init; }
}

public record VehicleReplacement
{
    public required ConsistRailVehicle Target { get; init; }

    public required RollingStockEntry Replacement { get; init; }
}
