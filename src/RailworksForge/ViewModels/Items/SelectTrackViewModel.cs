using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Assets;
using RailworksForge.Core.Routes;
using RailworksForge.Translations;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class SelectTrackViewModel(ITrackService tracks) : ViewModelBase
{
    public List<string> Providers { get; init; } = [];

    public RangeObservableCollection<string> Products { get; } = [];

    public RangeObservableCollection<Track> Tracks { get; } = [];

    [ObservableProperty]
    public partial string? SelectedProvider { get; set; }

    [ObservableProperty]
    public partial string? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial Track? SelectedTrack { get; set; }

    public required int TrackCount { get; init; }

    public required Blueprint RouteBlueprint { get; init; }

    partial void OnSelectedProviderChanged(string? value)
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

        _ = Loading.RunAsync(Strings.loading_products.CurrentValue, _ =>
        {
            var products = tracks.GetProducts(value);

            return Task.FromResult(products);
        }, Products.ReplaceWith);
    }

    partial void OnSelectedProductChanged(string? value)
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
            Strings.loading_track_blueprints.CurrentValue,
            token => tracks.GetTracks(provider, value, token),
            Tracks.ReplaceWith);
    }
}
