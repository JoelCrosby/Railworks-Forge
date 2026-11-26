namespace RailworksForge.Core.Scenarios;

public interface IScenarioDatabaseService
{
    event Action? Updated;

    Task Start(CancellationToken cancellationToken = default);

    ScenarioPlayerInfo GetScenario(string id);
}
