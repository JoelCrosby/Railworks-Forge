using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class RouteViewModel : ViewModelBase
{
    public string Id { get; init; }

    public string Name { get; init; }

    public string RoutePropertiesPath { get; }

    public string DirectoryPath { get; }

    public Route Model { get; }

    public PackagingType PackagingType { get; }

    [ObservableProperty]
    private Bitmap? _imageBitmap;

    public RouteViewModel(Route route)
    {
        Id = route.Id;
        Name = route.Name;
        RoutePropertiesPath = route.RoutePropertiesPath;
        DirectoryPath = route.DirectoryPath;
        PackagingType = route.PackagingType;
        Model = route;
    }

    public async Task LoadImage()
    {
        ImageBitmap = await Task.Run(GetImageBitmap);
    }

    private async Task<Bitmap?> GetImageBitmap()
    {
        if (await GetCompressedImageStream() is {} result)
        {
            return result;
        }

        return GetUnCompressedImageStream();
    }

    private Bitmap? GetUnCompressedImageStream()
    {
        var imageFiles = Directory.EnumerateFiles(DirectoryPath, "*.png", SearchOption.AllDirectories);
        var imagePath = imageFiles.FirstOrDefault(i => i.EndsWith("image.png", StringComparison.OrdinalIgnoreCase));
        var image = File.Exists(imagePath) ? File.OpenRead(imagePath) : null;

        return ReadBitmap(image);
    }

    private async Task<Bitmap?> GetCompressedImageStream()
    {
        var path = Directory
            .EnumerateFiles(DirectoryPath, "*.ap", SearchOption.AllDirectories)
            .FirstOrDefault(f => f.EndsWith("MainContent.ap", StringComparison.OrdinalIgnoreCase));

        if (path is null) return null;

        using var archive = ZipFile.Open(path, ZipArchiveMode.Read);

        var entry = archive.Entries
            .FirstOrDefault(entry => string.Equals(entry.Name, "Image.png", StringComparison.OrdinalIgnoreCase))?
            .Open();

        if (entry is null) return null;

        var image = new MemoryStream();
        await entry.CopyToAsync(image);
        image.Position = 0;

        return ReadBitmap(image);
    }

    private static Bitmap? ReadBitmap(Stream? stream)
    {
        try
        {
            return stream is null ? null : Bitmap.DecodeToWidth(stream, 256);
        }
        catch
        {
            return null;
        }
    }
}
