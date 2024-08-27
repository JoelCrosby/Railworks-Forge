namespace RailworksForge.Core.Models;

public class BrowserDirectory
{
    public List<BrowserDirectory> Subfolders => GetVehicleDirectories();

    public string Name { get; }
    public string FullPath { get; }
    public AssetBrowserLevel Level { get; }

    private string SubFolderPath { get; }

    private static readonly string AssetsDir = Paths.GetAssetsDirectory();

    public BrowserDirectory(string fullPath, string subFolderPath)
    {
        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
        Level = GetAssetLevel();
        SubFolderPath = subFolderPath;
    }

    private AssetBrowserLevel GetAssetLevel()
    {
        var relativeGamePath = FullPath.AsSpan().Slice(AssetsDir.Length, FullPath.Length - AssetsDir.Length);
        var parts = relativeGamePath.Count('/');

        return parts switch
        {
            1 => AssetBrowserLevel.Provider,
            2 => AssetBrowserLevel.Product,
            _ => AssetBrowserLevel.ProductAsset,
        };
    }

    private List<BrowserDirectory> GetVehicleDirectories()
    {
        if (Level > AssetBrowserLevel.Provider) return [];

        var directories = Directory.GetDirectories(FullPath);
        var vehicleDirectories = directories.Where(dir => Paths.EnumerateRailVehicles(dir, 1, SubFolderPath).Any());

        var sorted = vehicleDirectories
            .Select(dir => new BrowserDirectory(dir, SubFolderPath))
            .OrderBy(dir => Directory.EnumerateFileSystemEntries(dir.FullPath).Any())
            .ThenBy(dir => dir.Name);

        return [..sorted];
    }
}
