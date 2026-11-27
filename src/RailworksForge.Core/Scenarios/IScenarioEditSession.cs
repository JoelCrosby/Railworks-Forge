using RailworksForge.Core.Consists;
using RailworksForge.Core.Vehicles;

namespace RailworksForge.Core.Scenarios;

// Writing the session back to disk is left off the interface; edits are only applied through the editor.
public interface IScenarioEditSession
{
    Scenario Scenario { get; }

    bool HasChanges { get; }

    IReadOnlyList<ScenarioChange> Changes { get; }

    List<Consist> GetConsists(CancellationToken cancellationToken);

    List<ConsistRailVehicle> GetVehicles(Consist consist, CancellationToken cancellationToken);

    Task<string> GetConsistRailVehiclesXml(Consist consist);

    Task AddVehicle(Consist consist, RollingStockEntry vehicle);

    Task ReplaceVehicles(Consist consist, IEnumerable<ConsistRailVehicle> targets, RollingStockEntry replacement);

    Task DeleteVehicle(Consist consist, ConsistRailVehicle vehicle);

    Task MoveVehicle(Consist consist, ConsistRailVehicle vehicle, int newIndex);

    Task FlipVehicle(Consist consist, ConsistRailVehicle vehicle);

    Task ReplaceConsists(IEnumerable<Consist> targets, PreloadConsist replacement);

    Task DeleteConsists(IEnumerable<Consist> targets);
}
