using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

using Avalonia.Media.Imaging;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using Serilog;

namespace RailworksForge.Services;

// Misses are cached too: a failed lookup searches every archive in the product, and many rows share a blueprint.
public class ImageService
{
    private const int ThumbnailWidth = 256;

    private readonly ConcurrentDictionary<string, Bitmap?> _images = new(StringComparer.OrdinalIgnoreCase);

    public Bitmap? GetRouteImage(Route route)
    {
        return _images.GetOrAdd($"route:{route.DirectoryPath}", key => ReadSafely(key, () => ReadRouteImage(route)));
    }

    public Bitmap? GetBlueprintImage(Blueprint? blueprint)
    {
        if (blueprint is null)
        {
            return null;
        }

        return _images.GetOrAdd($"blueprint:{blueprint.BinaryPath}", key => ReadSafely(key, () => ReadBlueprintImage(blueprint)));
    }

    public Bitmap? GetConsistImage(Consist consist)
    {
        var leadVehicleImage = GetBlueprintImage(consist.LeadVehicle?.Blueprint);
        var lastVehicle = consist.Vehicles.LastOrDefault()?.Blueprint;

        return leadVehicleImage ?? GetBlueprintImage(lastVehicle);
    }

    private static Bitmap? ReadSafely(string key, Func<Bitmap?> read)
    {
        try
        {
            return read();
        }
        catch (Exception e)
        {
            Log.Debug(e, "failed to load image {Key}", key);

            return null;
        }
    }

    private static Bitmap? ReadRouteImage(Route route)
    {
        var routesDirectory = Paths.GetRoutesDirectory();
        var looseImageIdealPath = Path.Join(route.DirectoryPath, "RouteInformation", "Image.png");
        var looseImagePath = Paths.GetActualPathFromInsensitive(looseImageIdealPath, routesDirectory);
        var looseImage = DecodeFile(looseImagePath);

        if (looseImage is not null)
        {
            return looseImage;
        }

        var archiveIdealPath = Path.Join(route.DirectoryPath, "MainContent.ap");
        var archivePath = Paths.GetActualPathFromInsensitive(archiveIdealPath, routesDirectory);

        return archivePath is null ? null : DecodeArchiveEntry(archivePath, "RouteInformation/Image.png");
    }

    private static Bitmap? ReadBlueprintImage(Blueprint blueprint)
    {
        var blueprintDirectory = Path.GetDirectoryName(blueprint.BinaryPath);
        var looseImageIdealPath = Path.Join(blueprintDirectory, "LocoInformation", "Image.png");
        var looseImagePath = Paths.GetActualPathFromInsensitive(looseImageIdealPath);
        var looseImage = DecodeFile(looseImagePath);

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

        foreach (var archive in Directory.EnumerateFiles(productPath, "*.ap"))
        {
            var archiveImage = DecodeArchiveEntry(archive, entryPath);

            if (archiveImage is not null)
            {
                return archiveImage;
            }
        }

        return null;
    }

    private static Bitmap? DecodeFile(string? path)
    {
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);

            return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
        }
        catch (Exception e)
        {
            Log.Debug(e, "failed to decode image {Path}", path);

            return null;
        }
    }

    private static Bitmap? DecodeArchiveEntry(string archivePath, string entryPath)
    {
        var content = Archives.ReadFileBytes(archivePath, entryPath);

        if (content is null)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(content);

            return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
        }
        catch (Exception e)
        {
            Log.Debug(e, "failed to decode image {Path} in {Archive}", entryPath, archivePath);

            return null;
        }
    }
}
