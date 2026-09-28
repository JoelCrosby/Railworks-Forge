using AngleSharp.Dom;

using RailworksForge.Core.Commands;
using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.Exceptions;
using RailworksForge.Core.models;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

// Buffers consist edits against an in-memory copy of the scenario, so nothing reaches disk until Apply. Edits made
// here give new vehicles their real ids, so later edits in the same session can target them.
public sealed class ConsistEditSession
{
    private readonly Scenario _scenario;
    private readonly Consist _consist;
    private ConsistCommandContext _context;
    private DateTime? _savedVersion;

    private ConsistEditSession(Scenario scenario, Consist consist, ConsistCommandContext context)
    {
        _scenario = scenario;
        _consist = consist;
        _context = context;
        _savedVersion = scenario.SavedVersion;
    }

    public bool HasChanges { get; private set; }

    public static async Task<ConsistEditSession> Begin(Scenario scenario, Consist consist, CancellationToken cancellationToken)
    {
        using var scenarioLock = await ScenarioLocks.Acquire(scenario, cancellationToken);

        var context = await ConsistCommandRunner.LoadContext(scenario, useCache: true);

        return new ConsistEditSession(scenario, consist, context);
    }

    public List<ConsistRailVehicle> GetVehicles(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_consist.BlueprintId))
        {
            return [];
        }

        var vehicles = Scenario.GetServiceConsistVehicles(_context.ScenarioDocument, _consist);

        // Acquisition state is computed lazily from disk; resolve it here so the grid doesn't do it on the UI thread.
        foreach (var vehicle in vehicles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = vehicle.AcquisitionState;
        }

        return vehicles;
    }

    public Task AddVehicle(RollingStockEntry vehicle)
    {
        var request = new AddConsistVehicleRequest
        {
            Consist = _consist,
            VehicleToAdd = vehicle,
        };

        return Run(new AddConsistVehicle(request));
    }

    public Task ReplaceVehicles(IEnumerable<ConsistRailVehicle> targets, RollingStockEntry replacement)
    {
        var replacements = targets
            .Select(target => new VehicleReplacement
            {
                Replacement = replacement,
                Target = target,
            })
            .ToList();

        var request = new ReplaceVehiclesRequest
        {
            Consist = _consist,
            Replacements = replacements,
        };

        return Run(new ReplaceConsistVehicles(request));
    }

    public Task DeleteVehicle(ConsistRailVehicle vehicle)
    {
        var request = new DeleteConsistVehicleRequest
        {
            Consist = _consist,
            VehicleToDelete = vehicle,
        };

        return Run(new DeleteConsistVehicle(request));
    }

    public async Task Apply()
    {
        using var scenarioLock = await ScenarioLocks.Acquire(_scenario);

        if (_scenario.SavedVersion != _savedVersion)
        {
            throw new ScenarioChangedException();
        }

        await ConsistCommandRunner.Write(_context);

        _savedVersion = _scenario.SavedVersion;
        HasChanges = false;
    }

    // A command can fail part way through, e.g. on the second of several replacements, so it runs against a copy
    // that only replaces the buffered documents once the whole command has succeeded.
    private async Task Run(IConsistCommand command)
    {
        var working = _context with
        {
            ScenarioDocument = Copy(_context.ScenarioDocument),
            ScenarioPropertiesDocument = Copy(_context.ScenarioPropertiesDocument),
        };

        await command.Run(working);

        _context = working;
        HasChanges = true;
    }

    private static IDocument Copy(IDocument document)
    {
        return (IDocument)document.Clone(true);
    }
}
