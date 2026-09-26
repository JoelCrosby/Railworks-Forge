using System.Diagnostics;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

[DebuggerDisplay("{Name}")]
public partial class RouteViewModel : ObservableObject
{
    public string Id { get; init; }

    public string Name { get; init; }

    public string SearchIndex { get; init; }

    public string RoutePropertiesPath { get; }

    public string DirectoryPath { get; }

    public Route Model { get; }

    public PackagingType PackagingType { get; }

    [ObservableProperty]
    public partial Bitmap? ImageBitmap { get; set; }

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
}
