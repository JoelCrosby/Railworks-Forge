using AngleSharp.Dom;
using AngleSharp.Xml;

using RailworksForge.Core.Commands;
using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.models;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

// Buffers edits against an in-memory copy of the scenario, so nothing reaches disk until Apply. New vehicles get
// their real ids, so later edits in the same session can target them.
public sealed class ScenarioEditSession
{
    private ConsistCommandContext _context;
    private DateTime? _savedVersion;

    private ScenarioEditSession(ConsistCommandContext context)
    {
        _context = context;
        _savedVersion = context.Scenario.SavedVersion;
    }

    public Scenario Scenario => _context.Scenario;

    public bool HasChanges { get; private set; }

    public static async Task<ScenarioEditSession> Begin(Scenario scenario, CancellationToken cancellationToken)
    {
        using var scenarioLock = await ScenarioLocks.Acquire(scenario, cancellationToken);

        var updated = scenario.Refresh() ?? throw new InvalidOperationException("The scenario could not be loaded.");
        var propertiesDocument = await updated.GetPropertiesXmlDocument();
        var document = await updated.GetXmlDocument(false);

        var context = new ConsistCommandContext
        {
            Scenario = updated,
            ScenarioDocument = document,
            ScenarioPropertiesDocument = propertiesDocument,
        };

        return new ScenarioEditSession(context);
    }

    public List<Consist> GetConsists(CancellationToken cancellationToken)
    {
        // Content may have been added or removed since the scenario was last shown.
        Cache.ClearAcquisitionStates();

        return _context.ScenarioDocument
            .QuerySelectorAll("cConsist")
            .Select((element, index) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Consist.ParseScenarioConsist(element, index);
            })
            .OfType<Consist>()
            .ToList();
    }

    public List<ConsistRailVehicle> GetVehicles(Consist consist, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(consist.BlueprintId))
        {
            return [];
        }

        var vehicles = Scenario.GetServiceConsistVehicles(_context.ScenarioDocument, consist);

        // Acquisition state is computed lazily from disk; resolve it here so the grid doesn't do it on the UI thread.
        foreach (var vehicle in vehicles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = vehicle.AcquisitionState;
        }

        return vehicles;
    }

    public string GetConsistRailVehiclesXml(Consist consist)
    {
        var vehicles = Consist.GetServiceConsist(_context.ScenarioDocument, consist)?.QuerySelector("RailVehicles");

        if (vehicles is null)
        {
            throw new Exception("could not find consist in scenario bin");
        }

        return vehicles.ToXml();
    }

    public Task AddVehicle(Consist consist, RollingStockEntry vehicle)
    {
        var request = new AddConsistVehicleRequest
        {
            Consist = consist,
            VehicleToAdd = vehicle,
        };

        return Run(new AddConsistVehicle(request));
    }

    public Task ReplaceVehicles(Consist consist, IEnumerable<ConsistRailVehicle> targets, RollingStockEntry replacement)
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
            Consist = consist,
            Replacements = replacements,
        };

        return Run(new ReplaceConsistVehicles(request));
    }

    public Task DeleteVehicle(Consist consist, ConsistRailVehicle vehicle)
    {
        var request = new DeleteConsistVehicleRequest
        {
            Consist = consist,
            VehicleToDelete = vehicle,
        };

        return Run(new DeleteConsistVehicle(request));
    }

    public Task ReplaceConsists(IEnumerable<Consist> targets, PreloadConsist replacement)
    {
        var request = new ReplaceConsistRequest
        {
            Target = new TargetConsist(targets),
            PreloadConsist = replacement,
        };

        return Run(new ReplaceConsist(request));
    }

    public Task DeleteConsists(IEnumerable<Consist> targets)
    {
        return Run(new DeleteConsist(new TargetConsist(targets)));
    }

    public async Task Apply()
    {
        var scenario = _context.Scenario;

        using var scenarioLock = await ScenarioLocks.Acquire(scenario);

        if (scenario.SavedVersion != _savedVersion)
        {
            throw new ScenarioChangedException();
        }

        scenario.CreateBackup();

        await ScenarioWriter.WriteBinary(scenario, _context.ScenarioDocument);
        await ScenarioWriter.WritePropertiesDocument(scenario, _context.ScenarioPropertiesDocument);

        Cache.ClearScenarioCache(scenario);

        _context = _context with { Scenario = scenario.Refresh() ?? scenario };
        _savedVersion = _context.Scenario.SavedVersion;
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
