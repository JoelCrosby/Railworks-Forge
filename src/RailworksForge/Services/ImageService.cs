using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

using Avalonia.Media.Imaging;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Consists;
using RailworksForge.Core.Packaging;
using RailworksForge.Core.Routes;

using Serilog;

namespace RailworksForge.Services;

// Misses are cached too: a failed lookup searches every archive in the product, and many rows share a blueprint.
public class ImageService
{
    private const int ThumbnailWidth = 256;

    private readonly AssetImageService _assetImages;

    // Lazy so concurrent callers for the same image (e.g. a background row fill and a page load) decode it once.
    private readonly ConcurrentDictionary<string, Lazy<Bitmap?>> _images = new(StringComparer.OrdinalIgnoreCase);

    public ImageService(AssetImageService assetImages, PackageInstallService packages)
    {
        _assetImages = assetImages;

        packages.Installed += ClearMisses;
    }

    public Bitmap? GetRouteImage(Route route)
    {
        return GetOrRead($"route:{route.DirectoryPath}", () => _assetImages.GetRouteImage(route));
    }

    public Bitmap? GetBlueprintImage(Blueprint? blueprint)
    {
        if (blueprint is null)
        {
            return null;
        }

        return GetOrRead($"blueprint:{blueprint.BinaryPath}", () => _assetImages.GetBlueprintImage(blueprint));
    }

    public Bitmap? GetConsistImage(Consist consist)
    {
        var images = consist.ImageBlueprints.Select(GetBlueprintImage);

        return images.FirstOrDefault(image => image is not null);
    }

    private void ClearMisses()
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

    private Bitmap? GetOrRead(string key, Func<byte[]?> read)
    {
        var image = _images.GetOrAdd(key, _ => new Lazy<Bitmap?>(() => ReadSafely(key, read)));

        return image.Value;
    }

    private static Bitmap? ReadSafely(string key, Func<byte[]?> read)
    {
        try
        {
            var content = read();

            return content is null ? null : Decode(content);
        }
        catch (Exception e)
        {
            Log.Debug(e, "failed to load image {Key}", key);

            return null;
        }
    }

    private static Bitmap Decode(byte[] content)
    {
        using var stream = new MemoryStream(content);

        return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
    }
}
