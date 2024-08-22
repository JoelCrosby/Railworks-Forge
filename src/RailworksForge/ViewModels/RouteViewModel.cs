using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

[DebuggerDisplay("{Name}")]
public partial class RouteViewModel : ViewModelBase
{
    public string Id { get; init; }

    public string Name { get; init; }

    public string SearchIndex { get; init; }

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
        SearchIndex = Name.ToLowerInvariant();
        RoutePropertiesPath = route.RoutePropertiesPath;
        DirectoryPath = route.DirectoryPath;
        PackagingType = route.PackagingType;
        Model = route;
    }

    public async ValueTask LoadImage()
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

    private Bitmap? GetUnCompressedImageStream()
    {
        var idealPath = Path.Join(DirectoryPath, "RouteInformation", "Image.png");
        var imagePath = Paths.GetActualPathFromInsensitive(idealPath, Paths.GetRoutesDirectory());
        var image = File.Exists(imagePath) ? File.OpenRead(imagePath) : null;

        return image.ReadBitmap();
    }

    private async Task<Bitmap?> GetCompressedImageStream()
    {
        var idealPath = Path.Join(DirectoryPath, "MainContent.ap");
        var path = Paths.GetActualPathFromInsensitive(idealPath, Paths.GetRoutesDirectory());

        if (path is null) return null;

        return await Archives.GetBitmapStreamFromPath(path, "RouteInformation/Image.png", false);
    }
}
