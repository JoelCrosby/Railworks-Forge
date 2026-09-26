using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class SelectTrackViewModel(TrackService tracks) : ViewModelBase
{
    public List<DirectoryItem> Providers { get; init; } = [];

    public ObservableCollection<DirectoryItem> Products { get; } = [];

    public ObservableCollection<Track> Tracks { get; } = [];

    [ObservableProperty]
    public partial DirectoryItem? SelectedProvider { get; set; }

    [ObservableProperty]
    public partial DirectoryItem? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial Track? SelectedTrack { get; set; }

    public required int TrackCount { get; init; }

    public required Blueprint RouteBlueprint { get; init; }

    partial void OnSelectedProviderChanged(DirectoryItem? value)
    {
        Loading.Cancel();
        SelectedProduct = null;
        SelectedTrack = null;
        Products.Clear();
        Tracks.Clear();

        if (value is null)
        {
            return;
        }

        _ = Loading.RunAsync("Loading products…", _ =>
        {
            var products = tracks.GetProducts(value.Name);
            var items = products.ConvertAll(product => new DirectoryItem(product.Name, product));

            return Task.FromResult(items);
        }, Products.ReplaceWith);
    }

    partial void OnSelectedProductChanged(DirectoryItem? value)
    {
        Loading.Cancel();
        SelectedTrack = null;
        Tracks.Clear();

        var provider = SelectedProvider;

        if (value is null || provider is null)
        {
            return;
        }

        _ = Loading.RunAsync(
            "Loading track blueprints…",
            token => tracks.GetTracks(provider.Name, value.Directory, token),
            Tracks.ReplaceWith);
    }
}
