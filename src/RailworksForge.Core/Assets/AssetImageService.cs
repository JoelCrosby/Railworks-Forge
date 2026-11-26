using RailworksForge.Core.Common;
using RailworksForge.Core.Routes;

namespace RailworksForge.Core.Assets;

// A loose image takes precedence over one packed in the content's archives, matching how the game resolves them.
public class AssetImageService
{
    public byte[]? GetRouteImage(Route route)
    {
        var routesDirectory = Paths.GetRoutesDirectory();
        var looseImageIdealPath = Path.Join(route.DirectoryPath, "RouteInformation", "Image.png");
        var looseImagePath = Paths.GetActualPathFromInsensitive(looseImageIdealPath, routesDirectory);
        var looseImage = ReadFile(looseImagePath);

        if (looseImage is not null)
        {
            return looseImage;
        }

        var archiveIdealPath = Path.Join(route.DirectoryPath, "MainContent.ap");
        var archivePath = Paths.GetActualPathFromInsensitive(archiveIdealPath, routesDirectory);

        return archivePath is null ? null : Archives.ReadFileBytes(archivePath, "RouteInformation/Image.png");
    }

    public byte[]? GetBlueprintImage(Blueprint blueprint)
    {
        var blueprintDirectory = Path.GetDirectoryName(blueprint.BinaryPath);
        var looseImageIdealPath = Path.Join(blueprintDirectory, "LocoInformation", "Image.png");
        var looseImagePath = Paths.GetActualPathFromInsensitive(looseImageIdealPath);
        var looseImage = ReadFile(looseImagePath);

        if (looseImage is not null)
        {
            return looseImage;
        }

        var productPath = Paths.GetActualPathFromInsensitive(blueprint.ProductPath);

        if (productPath is null)
        {
            return null;
        }

        var entryPath = Path.Join(Path.GetDirectoryName(blueprint.BlueprintIdPath), "LocoInformation", "image.png");
        var archives = Directory.EnumerateFiles(productPath, "*.ap");
        var archiveImages = archives.Select(archive => Archives.ReadFileBytes(archive, entryPath));

        return archiveImages.FirstOrDefault(image => image is not null);
    }

    private static byte[]? ReadFile(string? path)
    {
        var hasFile = path is not null && File.Exists(path);

        return hasFile ? File.ReadAllBytes(path!) : null;
    }
}
