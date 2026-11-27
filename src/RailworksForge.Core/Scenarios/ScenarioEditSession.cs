using AngleSharp.Dom;
using AngleSharp.Xml;

using RailworksForge.Core.Common;
using RailworksForge.Core.Consists.Commands;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Exceptions;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Vehicles;

using Serilog;

namespace RailworksForge.Core.Scenarios;

// Buffers edits against an in-memory copy of the scenario, so nothing reaches disk until Apply. New vehicles get
// their real ids, so later edits in the same session can target them.
public sealed class ScenarioEditSession : IScenarioEditSession
{
    // Edits and Apply each start from the result of the one before, so they run one at a time.
    private readonly SemaphoreSlim _editLock = new(1, 1);
    private EditState _state;
    private DateTime? _savedVersion;
    private ScenarioDetails _savedDetails;
    private List<ScenarioChange> _changes = [];
    private bool _hasBinaryChanges;

    private ScenarioEditSession(EditState state)
    {
        _state = state;
        _savedVersion = state.Scenario.SavedVersion;
        _savedDetails = ScenarioDetails.Read(state.ScenarioPropertiesDocument);
    }

    public Scenario Scenario => _state.Scenario;

    public bool HasChanges => _changes.Count > 0;

    public IReadOnlyList<ScenarioChange> Changes => _changes;

    // Consists are listed straight from Scenario.bin, which takes milliseconds. Parsing the full document takes
    // seconds for a large scenario, so it carries on in the background and only edits wait for it.
    public static async Task<ScenarioEditSession> Begin(Scenario scenario, CancellationToken cancellationToken)
    {
        using var scenarioLock = await ScenarioLocks.Acquire(scenario, cancellationToken);

        var updated = scenario.Refresh() ?? throw new InvalidOperationException("The scenario could not be loaded.");
        var propertiesDocument = await updated.GetPropertiesXmlDocument();
        var binary = await updated.ReadBinary(cancellationToken);
        var consists = ScenarioNode.ReadConsists(binary, cancellationToken);

        var state = new EditState
        {
            Scenario = updated,
            ScenarioDocument = LoadDocument(updated, updated.SavedVersion),
            ScenarioPropertiesDocument = propertiesDocument,
            Consists = new Lazy<List<ScenarioNode>>(() => consists),
        };

        return new ScenarioEditSession(state);
    }

    public List<Consist> GetConsists(CancellationToken cancellationToken)
    {
        // Content may have been added or removed since the scenario was last shown.
        Cache.ClearAcquisitionStates();

        return _state.Consists.Value
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

        var vehicles = Scenario.GetServiceConsistVehicles(_state.Consists.Value, consist);

        // Acquisition state is computed lazily from disk; resolve it here so the grid doesn't do it on the UI thread.
        foreach (var vehicle in vehicles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = vehicle.AcquisitionState;
        }

        return vehicles;
    }

    public async Task<string> GetConsistRailVehiclesXml(Consist consist)
    {
        await _editLock.WaitAsync();

        try
        {
            var document = await GetDocument();
            var vehicles = Consist.GetServiceConsist(document, consist)?.QuerySelector("RailVehicles");

            if (vehicles is null)
            {
                throw new Exception("could not find consist in scenario bin");
            }

            return vehicles.ToXml();
        }
        finally
        {
            _editLock.Release();
        }
    }

    public Task AddVehicle(Consist consist, RollingStockEntry vehicle)
    {
        var request = new AddConsistVehicleRequest
        {
            Consist = consist,
            VehicleToAdd = vehicle,
        };

        var change = new ScenarioChange(ScenarioChangeKind.AddVehicle, consist.ServiceName, 1);

        return Run(new AddConsistVehicle(request), change);
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

        var change = new ScenarioChange(ScenarioChangeKind.ReplaceVehicles, consist.ServiceName, replacements.Count);

        return Run(new ReplaceConsistVehicles(request), change);
    }

    public Task DeleteVehicle(Consist consist, ConsistRailVehicle vehicle)
    {
        var request = new DeleteConsistVehicleRequest
        {
            Consist = consist,
            VehicleToDelete = vehicle,
        };

        var change = new ScenarioChange(ScenarioChangeKind.DeleteVehicle, consist.ServiceName, 1);

        return Run(new DeleteConsistVehicle(request), change);
    }

    public Task MoveVehicle(Consist consist, ConsistRailVehicle vehicle, int newIndex)
    {
        var request = new MoveConsistVehicleRequest
        {
            Consist = consist,
            Vehicle = vehicle,
            NewIndex = newIndex,
        };

        var change = new ScenarioChange(ScenarioChangeKind.MoveVehicle, consist.ServiceName, 1);

        return Run(new MoveConsistVehicle(request), change);
    }

    public Task FlipVehicle(Consist consist, ConsistRailVehicle vehicle)
    {
        var request = new FlipConsistVehicleRequest
        {
            Consist = consist,
            Vehicle = vehicle,
        };

        var change = new ScenarioChange(ScenarioChangeKind.FlipVehicle, consist.ServiceName, 1);

        return Run(new FlipConsistVehicle(request), change);
    }

    public Task ReplaceConsists(IEnumerable<Consist> targets, PreloadConsist replacement)
    {
        var targetList = targets.ToList();
        var request = new ReplaceConsistRequest
        {
            Target = new TargetConsist(targetList),
            PreloadConsist = replacement,
        };

        var change = new ScenarioChange(ScenarioChangeKind.ReplaceConsists, DescribeTargets(targetList), targetList.Count);

        return Run(new ReplaceConsist(request), change);
    }

    public Task DeleteConsists(IEnumerable<Consist> targets)
    {
        var targetList = targets.ToList();
        var change = new ScenarioChange(ScenarioChangeKind.DeleteConsists, DescribeTargets(targetList), targetList.Count);

        return Run(new DeleteConsist(new TargetConsist(targetList)), change);
    }

    public ScenarioDetails GetDetails()
    {
        return ScenarioDetails.Read(_state.ScenarioPropertiesDocument);
    }

    // Editing the details again replaces the earlier details change, so the pending changes list them once.
    public async Task UpdateDetails(ScenarioDetails details)
    {
        await _editLock.WaitAsync();

        try
        {
            var state = _state;
            var current = ScenarioDetails.Read(state.ScenarioPropertiesDocument);

            if (details == current)
            {
                return;
            }

            var properties = Copy(state.ScenarioPropertiesDocument);
            details.Write(properties, current);

            _state = state with
            {
                Scenario = details.ApplyTo(state.Scenario),
                ScenarioPropertiesDocument = properties,
            };

            var otherChanges = _changes.Where(change => change.Kind is not ScenarioChangeKind.EditDetails);
            var isBackToSaved = details == _savedDetails;
            var detailsChange = new ScenarioChange(ScenarioChangeKind.EditDetails, string.Empty, 1);

            _changes = isBackToSaved ? [..otherChanges] : [..otherChanges, detailsChange];
        }
        finally
        {
            _editLock.Release();
        }
    }

    private static string DescribeTargets(List<Consist> targets)
    {
        var isSingleTarget = targets.Count is 1;

        return isSingleTarget ? targets[0].ServiceName : string.Empty;
    }

    public async Task Apply()
    {
        await _editLock.WaitAsync();

        try
        {
            await ApplyEdits();
        }
        finally
        {
            _editLock.Release();
        }
    }

    // Scenario.bin is only rewritten when a consist was edited; converting it back to a binary is the slow part.
    private async Task ApplyEdits()
    {
        var hasBinaryChanges = _hasBinaryChanges;
        var document = hasBinaryChanges ? await GetDocument() : null;
        var state = _state;
        var scenario = state.Scenario;

        using var scenarioLock = await ScenarioLocks.Acquire(scenario);

        if (scenario.SavedVersion != _savedVersion)
        {
            throw new ScenarioChangedException();
        }

        scenario.CreateBackup();

        if (document is not null)
        {
            await ScenarioWriter.WriteBinary(scenario, document);
        }

        await ScenarioWriter.WritePropertiesDocument(scenario, state.ScenarioPropertiesDocument);

        Cache.ClearScenarioCache(scenario);

        _state = state with { Scenario = scenario.Refresh() ?? scenario };
        _savedVersion = _state.Scenario.SavedVersion;
        _savedDetails = ScenarioDetails.Read(state.ScenarioPropertiesDocument);
        _changes = [];
        _hasBinaryChanges = false;
    }

    // A command can fail part way through, e.g. on the second of several replacements, so it runs against a copy
    // that only replaces the buffered documents once the whole command has succeeded.
    private async Task Run(IConsistCommand command, ScenarioChange change)
    {
        await _editLock.WaitAsync();

        try
        {
            var document = await GetDocument();
            var state = _state;
            var working = new ConsistCommandContext
            {
                Scenario = state.Scenario,
                ScenarioDocument = Copy(document),
                ScenarioPropertiesDocument = Copy(state.ScenarioPropertiesDocument),
            };

            await command.Run(working);

            // Replaced in one assignment, so a reader never pairs the edited document with the consists listed before
            // it. The consists are only copied out of the document when they are next listed.
            _state = state with
            {
                ScenarioDocument = Task.FromResult(working.ScenarioDocument),
                ScenarioPropertiesDocument = working.ScenarioPropertiesDocument,
                Consists = new Lazy<List<ScenarioNode>>(() => ScenarioNode.ReadConsists(working.ScenarioDocument)),
            };
            _changes = [.._changes, change];
            _hasBinaryChanges = true;
        }
        finally
        {
            _editLock.Release();
        }
    }

    // Called with the edit lock held. A load that failed is started again, so one failure doesn't break every
    // later edit in the session.
    private Task<IDocument> GetDocument()
    {
        var state = _state;
        var hasFailed = state.ScenarioDocument.IsFaulted || state.ScenarioDocument.IsCanceled;

        if (hasFailed)
        {
            state = state with { ScenarioDocument = LoadDocument(state.Scenario, _savedVersion) };
            _state = state;
        }

        return state.ScenarioDocument;
    }

    // Runs in the background after Begin has released the scenario lock. The consists were listed from the binary
    // while the lock was held, so a scenario saved in between would no longer match them and is reported as changed.
    private static Task<IDocument> LoadDocument(Scenario scenario, DateTime? version)
    {
        var document = Task.Run(async () =>
        {
            var loaded = await scenario.GetXmlDocument();

            if (scenario.SavedVersion != version)
            {
                throw new ScenarioChangedException();
            }

            return loaded;
        });

        _ = document.ContinueWith(
            task => Log.Warning(task.Exception, "Failed to load scenario document for {Scenario}", scenario.Name),
            TaskContinuationOptions.OnlyOnFaulted);

        return document;
    }

    private static IDocument Copy(IDocument document)
    {
        return (IDocument)document.Clone(true);
    }

    private sealed record EditState
    {
        public required Scenario Scenario { get; init; }

        // Still loading in the background until an edit or Apply first needs it.
        public required Task<IDocument> ScenarioDocument { get; init; }

        public required IDocument ScenarioPropertiesDocument { get; init; }

        public required Lazy<List<ScenarioNode>> Consists { get; init; }
    }
}
