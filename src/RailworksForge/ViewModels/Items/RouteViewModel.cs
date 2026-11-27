using System.Collections.Generic;
using System.Diagnostics;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Packaging;
using RailworksForge.Core.Routes;

namespace RailworksForge.ViewModels;

[DebuggerDisplay("{Name}")]
public partial class RouteViewModel : ObservableObject
{
    public string Id { get; }

    [ObservableProperty]
    public partial string Name { get; private set; }

    [ObservableProperty]
    public partial string SearchIndex { get; private set; }

    public string RoutePropertiesPath { get; }

    public string DirectoryPath { get; }

    // Compared by reference: a route equals any other with the same id, so a renamed one would otherwise be ignored.
    public Route Model
    {
        get;
        private set => SetProperty(ref field, value, ReferenceEqualityComparer.Instance);
    }

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

    public void Update(Route route)
    {
        Name = route.Name;
        SearchIndex = Name.ToLowerInvariant();
        Model = route;
    }
}
