namespace RailworksForge.Core.Models;

public class BrowserDirectory
{
    public List<BrowserDirectory> Subfolders => GetVehicleDirectories();

    public string Name { get; }
    public AssetDirectory AssetDirectory { get; }

    private Func<ProductDirectory, bool> ChildPredicate { get; }

    public BrowserDirectory(AssetDirectory baseDirectory, Func<ProductDirectory, bool> childPredicate)
    {
        AssetDirectory = baseDirectory;
        Name = baseDirectory.Name;
        ChildPredicate = childPredicate;
    }

    private List<BrowserDirectory> GetVehicleDirectories()
    {
        if (AssetDirectory is ProviderDirectory providerDirectory)
        {
            return providerDirectory.Products
                .Where(ChildPredicate)
                .Select(e => new BrowserDirectory(e, ChildPredicate))
                .ToList();
        }

        return [];
    }

    public static List<BrowserDirectory> ViewAllBrowser()
    {
        return AssetDatabase.Open.ProviderDirectories
            .Select(e => new BrowserDirectory(e, _ => true))
            .ToList();
    }
}
