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

    // Lazy so concurrent callers for the same image (e.g. a background row fill and a page load) decode it once.
    private readonly ConcurrentDictionary<string, Lazy<Bitmap?>> _images = new(StringComparer.OrdinalIgnoreCase);

    public void ClearMisses()
    {
        var missingKeys = _images
            .Where(pair => pair.Value is { IsValueCreated: true, Value: null })
            .Select(pair => pair.Key)
            .ToList();

        foreach (var key in missingKeys)
        {
            _images.TryRemove(key, out _);
        }
    }

    public Bitmap? GetRouteImage(Route route)
    {
        return GetOrRead($"route:{route.DirectoryPath}", () => ReadRouteImage(route));
    }

    public Bitmap? GetBlueprintImage(Blueprint? blueprint)
    {
        if (blueprint is null)
        {
            return null;
        }

        return GetOrRead($"blueprint:{blueprint.BinaryPath}", () => ReadBlueprintImage(blueprint));
    }

    public Bitmap? GetConsistImage(Consist consist)
    {
        var leadVehicleImage = GetBlueprintImage(consist.LeadVehicle?.Blueprint);
        var lastVehicle = consist.Vehicles.LastOrDefault()?.Blueprint;

        return leadVehicleImage ?? GetBlueprintImage(lastVehicle);
    }

    private Bitmap? GetOrRead(string key, Func<Bitmap?> read)
    {
        var image = _images.GetOrAdd(key, _ => new Lazy<Bitmap?>(() => ReadSafely(key, read)));

        return image.Value;
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
