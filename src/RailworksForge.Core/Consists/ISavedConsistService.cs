using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Consists;

public interface ISavedConsistService
{
    Task Save(IScenarioEditor editor, Consist consist, string name, CancellationToken cancellationToken);

    List<SavedConsist> GetSavedConsists();
}
