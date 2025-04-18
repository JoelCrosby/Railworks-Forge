using System.Collections.Concurrent;
using System.IO;

using Avalonia.Media.Imaging;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.Util;

public static class BitmapUtils
{
    public static Bitmap? GetImageBitmap(Blueprint? blueprint)
    {
        if (GetUnCompressedImageStream(blueprint) is {} result)
        {
            return result;
        }

        return GetCompressedImageStream(blueprint);
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
            try
            {
                return File.OpenRead(path);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        return null;
    }

    private static Bitmap? GetCompressedImageStream(Blueprint? blueprint)
    {
        if (blueprint is null) return null;

        var path = Paths.GetActualPathFromInsensitive(blueprint.ProductPath);

        if (path is null) return null;

        var archives = Directory.EnumerateFiles(path, "*.ap");

        foreach (var archive in archives)
        {
            var directory = Path.GetDirectoryName(blueprint.BlueprintIdPath);
            var idealPath = Path.Join(directory, "LocoInformation", "image.png");
            var result = Archives.GetBitmapStreamFromPath(archive, idealPath);

            if (result is not null) return result;
        }

        return null;
    }
}
