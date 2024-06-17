using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class PersistenceService
{
    public static void SaveConsist(string name, Consist consist)
    {
        const string filename = "consists";

        var consists = ApplicationConfig.GetConfigFromPath<List<SavedConsist>>(filename, []);

        consists.Add(new SavedConsist
        {
            Name = name,
            Consist = consist,
        });

        ApplicationConfig.SaveConfig(filename, consists);
    }
}
