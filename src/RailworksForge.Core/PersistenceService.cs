using RailworksForge.Core.Config;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

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
