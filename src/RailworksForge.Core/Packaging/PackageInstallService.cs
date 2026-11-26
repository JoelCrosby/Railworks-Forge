using RailworksForge.Core.Assets;
using RailworksForge.Core.Common;
using RailworksForge.Core.Routes;

namespace RailworksForge.Core.Packaging;

public class PackageInstallService(AssetDirectoryTreeService directoryTree, RouteService routes)
{
    // Raised on the installing thread once installed assets are visible to fresh lookups.
    public event Action? Installed;

    public async Task Install(IReadOnlyList<string> packagePaths, IProgress<InstallProgress> progress)
    {
        var packager = new Packager();

        try
        {
            foreach (var packagePath in packagePaths)
            {
                await packager.InstallPackage(packagePath, progress);
            }
        }
        finally
        {
            // A failed install can still have written some files, so caches are dropped either way.
            Cache.ClearAssetCaches();
            directoryTree.Invalidate();
            routes.InvalidateRoutes();
            Installed?.Invoke();
        }
    }
}
