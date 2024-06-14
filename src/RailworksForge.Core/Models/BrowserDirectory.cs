namespace RailworksForge.Core.Models;

public class BrowserDirectory
{
    public Task<List<BrowserDirectory>> Subfolders => Task.Run(GetVehicleDirectories);

    public string Name { get; }
    public string FullPath { get; }
    public AssetBrowserLevel Level { get; }

    public BrowserDirectory(string fullPath)
    {
        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
        Level = GetAssetLevel();
    }

    private AssetBrowserLevel GetAssetLevel()
    {
        var relativeGamePath = FullPath[Paths.GetAssetsDirectory().Length..];

        return relativeGamePath.Split("/").Length switch
        {
            2 => AssetBrowserLevel.Author,
            3 => AssetBrowserLevel.Package,
            _ => AssetBrowserLevel.PackageArchive,
        };
    }

    private List<BrowserDirectory> GetVehicleDirectories()
    {
        var directories = Directory.GetDirectories(FullPath);
        var vehicleDirectories = directories.Where(dir => Paths.EnumerateRailVehicles(dir, 1).Any());

        var sorted = vehicleDirectories
            .Select(dir => new BrowserDirectory(dir))
            .OrderBy(dir => Directory.EnumerateFileSystemEntries(dir.FullPath).Any())
            .ThenBy(dir => dir.Name);

        return [..sorted];
    }
}
