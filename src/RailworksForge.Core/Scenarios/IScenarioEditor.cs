using System.ComponentModel;

namespace RailworksForge.Core.Scenarios;

public interface IScenarioEditor : INotifyPropertyChanged
{
    Scenario Scenario { get; }

    bool HasPendingChanges { get; }

    Task<IScenarioEditSession> GetSession(CancellationToken cancellationToken);

    Task<bool> Apply(CancellationToken cancellationToken);

    void Discard();

    void Update();
}
