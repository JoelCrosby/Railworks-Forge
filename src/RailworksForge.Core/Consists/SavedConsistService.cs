using RailworksForge.Core.Config;
using RailworksForge.Core.Scenarios;

namespace RailworksForge.Core.Consists;

public class SavedConsistService : ISavedConsistService
{
    private const string ConsistsFilename = "consists";

    // Saves the consist as it stands in the editor, including edits that have not been applied yet.
    public async Task Save(IScenarioEditor editor, Consist consist, string name, CancellationToken cancellationToken)
    {
        var session = await editor.GetSession(cancellationToken);
        var consistElement = session.GetConsistRailVehiclesXml(consist);
        var savedConsist = new SavedConsist
        {
            Name = name,
            LocomotiveName = consist.LocomotiveName,
            ConsistElement = consistElement,
        };
        var savedConsists = GetSavedConsists();

        savedConsists.Add(savedConsist);

        Configuration.SaveConfig(ConsistsFilename, savedConsists);
    }

    public List<SavedConsist> GetSavedConsists()
    {
        return Configuration.GetConfigFromPath<List<SavedConsist>>(ConsistsFilename, []);
    }
}
