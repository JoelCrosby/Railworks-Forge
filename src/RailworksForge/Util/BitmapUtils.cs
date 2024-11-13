using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Util;

public static class BitmapUtils
{
    public static async Task<Bitmap?> GetImageBitmap(Blueprint? blueprint)
    {
        if (GetUnCompressedImageStream(blueprint) is {} result)
        {
            return result;
        }

        return await GetCompressedImageStream(blueprint);
    }

    private static readonly ConcurrentDictionary<string, Bitmap?> BitmapCache = new();

    private static Bitmap? GetUnCompressedImageStream(Blueprint? blueprint)
    {
        if (blueprint is null) return null;

        if (BitmapCache.TryGetValue(blueprint.BinaryPath, out var cached))
        {
            return cached;
        }

        var blueprintPath = Path.GetDirectoryName(blueprint.BinaryPath);
        var idealPath = Path.Join(blueprintPath, "LocoInformation", "Image.png");
        var imagePath = Paths.GetActualPathFromInsensitive(idealPath);
        var image = GetImageStream(imagePath);

        var result = image.ReadBitmap();

        if (result is null) return null;

        BitmapCache.TryAdd(blueprint.BinaryPath, result);

        return result;
    }

    private static FileStream? GetImageStream(string? imagePath)
    {
        if (imagePath is null) return null;

        if (Paths.GetActualPathFromInsensitive(imagePath) is { } path)
        {
            return File.OpenRead(path);
        }

        return null;
    }

    private static async Task<Bitmap?> GetCompressedImageStream(Blueprint? blueprint)
    {
        if (blueprint is null) return null;

        var path = Paths.GetActualPathFromInsensitive(blueprint.ProductPath);

        if (path is null) return null;

        var archives = Directory.EnumerateFiles(path, "*.ap");

        foreach (var archive in archives)
        {
            var directory = Path.GetDirectoryName(blueprint.BlueprintIdPath);
            var idealPath = Path.Join(directory, "LocoInformation", "Image.png");
            var result = await Archives.GetBitmapStreamFromPath(archive, idealPath, false);

            if (result is not null) return result;
        }

        return null;
    }
}
