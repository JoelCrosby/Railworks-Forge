using System.ComponentModel;

using RailworksForge.Core.Exceptions;

namespace RailworksForge.Core.Scenarios;

// Shared by a scenario's page and its consist pages, so edits made on either are applied or discarded together.
public class ScenarioEditor : IScenarioEditor
{
    private readonly Lock _sessionLock = new();
    private Task<ScenarioEditSession>? _session;

    public ScenarioEditor(Scenario scenario)
    {
        Scenario = scenario;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Scenario Scenario { get; private set; }

    public bool HasPendingChanges
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasPendingChanges)));
        }
    }

    public IReadOnlyList<ScenarioChange> PendingChanges
    {
        get;
        private set
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingChanges)));
        }
    } = [];

    public async Task<IScenarioEditSession> GetSession(CancellationToken cancellationToken)
    {
        return await GetEditSession(cancellationToken);
    }

    // Started without the caller's token, so a page cancelling its load doesn't leave a cancelled session behind.
    private Task<ScenarioEditSession> GetEditSession(CancellationToken cancellationToken)
    {
        lock (_sessionLock)
        {
            if (_session is null || _session.IsFaulted || _session.IsCanceled)
            {
                _session = ScenarioEditSession.Begin(Scenario, CancellationToken.None);
            }

            return _session.WaitAsync(cancellationToken);
        }
    }

    // False when the scenario changed on disk after editing began, so the edits were not saved.
    public async Task<bool> Apply(CancellationToken cancellationToken)
    {
        var session = await GetEditSession(cancellationToken);

        try
        {
            await session.Apply();

            return true;
        }
        catch (ScenarioChangedException)
        {
            return false;
        }
    }

    public void Discard()
    {
        lock (_sessionLock)
        {
            _session = null;
        }

        PendingChanges = [];
        HasPendingChanges = false;
    }

    // Called on the UI thread after the session is loaded or changed, since it raises change notifications.
    public void Update()
    {
        if (_session is not { IsCompletedSuccessfully: true } loaded)
        {
            return;
        }

        Scenario = loaded.Result.Scenario;
        PendingChanges = loaded.Result.Changes;
        HasPendingChanges = loaded.Result.HasChanges;
    }
}
