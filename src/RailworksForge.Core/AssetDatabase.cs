using Serilog;

namespace RailworksForge.Core;

public abstract record AssetDirectory
{
    public required string Name { get; init; }

    public required string Path { get; init; }
}

public record ProductDirectory : AssetDirectory
{
    public required bool ContainsRailVehicles { get; init; }

    public required bool ContainsPreloadData { get; init; }
}

public record ProviderDirectory : AssetDirectory
{
    public required List<ProductDirectory> Products { get; init; }
}

public class AssetDatabase
{
    public required List<ProviderDirectory> ProviderDirectories { get; init; }

    public static readonly AssetDatabase Open = Build();

    private static AssetDatabase Build()
    {
        var assetsDirectory = Paths.GetAssetsDirectory();
        var providerDirectories = Directory.EnumerateDirectories(assetsDirectory, "*", SearchOption.TopDirectoryOnly).ToList();

        var providers = providerDirectories.Select(e => new ProviderDirectory
        {
            Name = Path.GetFileName(e),
            Path = e,
            Products = GetProductDirectories(e),
        });

        return new AssetDatabase
        {
            ProviderDirectories = providers.OrderBy(p => p.Name).ToList(),
        };
    }

    private static List<ProductDirectory> GetProductDirectories(string e)
    {
        var products = new List<ProductDirectory>();
        var productDirectories = Directory.EnumerateDirectories(e, "*", SearchOption.TopDirectoryOnly);

        foreach (var productDirectory in productDirectories)
        {
            var children = Directory
                .EnumerateFileSystemEntries(productDirectory, "*", SearchOption.TopDirectoryOnly)
                .ToList();

            var childNames = children.Select(Path.GetFileName).ToList();

            var containsPreloadData = childNames.Any(child => string.Equals(child, "PreloadData", StringComparison.OrdinalIgnoreCase));
            var containsRailVehicles = childNames.Any(child => string.Equals(child, "RailVehicles", StringComparison.OrdinalIgnoreCase));

            var archives = children.Where(s => s.EndsWith(".ap", StringComparison.OrdinalIgnoreCase));

            foreach (var archive in archives)
            {
                try
                {
                    if (Archives.TopLevelDirectoryExists(archive, "PreloadData"))
                    {
                        containsPreloadData = true;
                    }

                    if (Archives.TopLevelDirectoryExists(archive, "RailVehicles"))
                    {
                        containsRailVehicles = true;
                    }

                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to open archive @ '{Archive}' for {ProductDirectory}", archive, productDirectory);
                }
            }

            products.Add(new ProductDirectory
            {
                Name = Path.GetFileName(productDirectory),
                Path = productDirectory,
                ContainsPreloadData = containsPreloadData,
                ContainsRailVehicles = containsRailVehicles,
            });
        }

        return products.OrderBy(p => p.Name).ToList();
    }
}
