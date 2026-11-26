using RailworksForge.Core.Config;

namespace RailworksForge.Core.Consists;

public class PersistenceService
{
    private const string ConsistsFilename = "consists";

    public static void SaveConsist(SavedConsist consist)
    {
        var consists = Configuration.GetConfigFromPath<List<SavedConsist>>(ConsistsFilename, []);

        consists.Add(consist);

        Configuration.SaveConfig(ConsistsFilename, consists);
    }

    public static List<SavedConsist> GetConsists()
    {
        return Configuration.GetConfigFromPath<List<SavedConsist>>(ConsistsFilename, []);
    }
}
