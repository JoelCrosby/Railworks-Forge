using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class PreloadConsistViewModel : ViewModelBase
{
    public PreloadConsist Consist { get; }

    [ObservableProperty]
    private Bitmap? _imageBitmap;

    public PreloadConsistViewModel(PreloadConsist consist)
    {
        Consist = consist;
    }

    public async Task LoadImage()
    {
        ImageBitmap = await Task.Run(GetImageBitmap);
    }

    private async Task<Bitmap?> GetImageBitmap()
    {
        if (GetUnCompressedImageStream() is {} result)
        {
            return result;
        }

        return await GetCompressedImageStream();
    }

    private static readonly ConcurrentDictionary<string, Bitmap?> BitmapCache = new();

    private Bitmap? GetUnCompressedImageStream()
    {
        var consist = Consist.ConsistEntries.FirstOrDefault();

        if (consist is null) return null;

        if (BitmapCache.TryGetValue(consist.BinaryPath, out var cached))
        {
            return cached;
        }

        var blueprintPath = Path.GetDirectoryName(consist.BinaryPath);
        var idealPath = Path.Join(blueprintPath, "LocoInformation", "Image.png");
        var imagePath = Paths.GetActualPathFromInsensitive(idealPath);
        var image = File.Exists(imagePath) ? File.OpenRead(imagePath) : null;

        var result = image.ReadBitmap();

        BitmapCache.TryAdd(consist.BinaryPath, result);

        return result;
    }

    private async Task<Bitmap?> GetCompressedImageStream()
    {
        var consist = Consist.ConsistEntries.FirstOrDefault();

        if (consist is null) return null;

        var path = Paths.GetActualPathFromInsensitive(consist.ProductPath);

        if (path is null) return null;

        var archives = Directory.EnumerateFiles(path, "*.ap");

        foreach (var archive in archives)
        {
            var directory = Path.GetDirectoryName(consist.BlueprintIdPath);
            var idealPath = Path.Join(directory, "LocoInformation", "Image.png");
            var result = await Archives.GetStreamFromPath(archive, idealPath, false);

            if (result is not null) return result;
        }

        return null;
    }
}
