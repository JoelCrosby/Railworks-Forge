using RailworksForge.Core.External;
using RailworksForge.Core.Models;

namespace RailworksForge.Core;

public class PreloadService
{
    public async Task<List<RailVehicle>> GetRailVehicles()
    {
        var vehicles = new HashSet<RailVehicle>();
        var assetsDir = Paths.GetAssetsDirectory();
        var dtgDir = Path.Join(assetsDir, "DTG");

        var bins = Directory.EnumerateFiles(dtgDir, "*.bin", SearchOption.AllDirectories).Where(IsVehicleBinPath).ToList();

        foreach (var bin in bins)
        {
            var result = await Serz.Convert(bin);
            var document = result.Parse();

            var locoName = document.QuerySelector("cConsistBlueprint LocoName English")?.TextContent;
            var displayName = document.QuerySelector("cConsistBlueprint DisplayName English")?.TextContent;

            File.Delete(result.OutputPath);

            if (string.IsNullOrEmpty(locoName) || string.IsNullOrEmpty(displayName))
            {
                continue;
            }

            vehicles.Add(new RailVehicle
            {
                LocoName = locoName,
                DisplayName = displayName,
            });
        }

        return vehicles.ToList();
    }

    private static bool IsVehicleBinPath(string path)
    {
        return path.Contains("/PreLoad/");
    }
}
