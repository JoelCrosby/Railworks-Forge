using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ReplaceTrackViewModel : ViewModelBase
{
    [ObservableProperty]
    private Route _route;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private List<SelectTrackViewModel> _routeTracks;

    public ReactiveCommand<Unit, ReplaceTracksRequest> ReplaceTracksCommand { get; }

    public ReplaceTrackViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        RouteTracks = [];

        ReplaceTracksCommand = ReactiveCommand.Create(() =>
        {
            return new ReplaceTracksRequest
            {
                Replacements = RouteTracks.ConvertAll(r => new TrackReplacement
                {
                    Blueprint = r.RouteBlueprint,
                    ReplacementBlueprint = r.SelectedTrack?.Blueprint,
                }),
            };
        });

        if (Design.IsDesignMode is false)
        {
            Observable.FromAsync(GetRouteTracks, RxApp.TaskpoolScheduler).Subscribe(tracks =>
            {
                Dispatcher.UIThread.Post(() => RouteTracks = tracks);
            });
        }
    }

    private async Task<List<SelectTrackViewModel>> GetRouteTracks()
    {
        var blueprints = await Route.GetTrackBlueprints();

        var  models = blueprints
            .ToHashSet()
            .Where(blueprint => string.IsNullOrEmpty(blueprint.BlueprintId) is false)
            .Select(track => new SelectTrackViewModel { RouteBlueprint = track })
            .ToList();

        Dispatcher.UIThread.Post(() => IsLoading = false);

        return models;
    }
}

public record DirectoryItem(string Name, DirectoryInfo Directory)
{
    public override string ToString() => Name;
}

public partial class SelectTrackViewModel : ViewModelBase
{
    public static List<DirectoryItem> Providers => Paths.GetAssetProviders().ConvertAll(d => new DirectoryItem(d.Name, d));

    [ObservableProperty]
    private ObservableCollection<DirectoryItem> _products = [];

    [ObservableProperty]
    private ObservableCollection<Track> _tracks = [];

    [ObservableProperty]
    private DirectoryItem? _selectedProvider;

    [ObservableProperty]
    private DirectoryItem? _selectedProduct;

    [ObservableProperty]
    private Track? _selectedTrack;

    public required Blueprint RouteBlueprint { get; init; }

    partial void OnSelectedProviderChanged(DirectoryItem? value)
    {
        if (value is null) return;

        var products = Paths.GetAssetProviderProducts(value.Name);
        var items = products.ConvertAll(p => new DirectoryItem(p.Name, p));

        Products = new ObservableCollection<DirectoryItem>(items);
    }

    partial void OnSelectedProductChanged(DirectoryItem? item)
    {
        if (item is null || SelectedProvider is null || SelectedProduct is null)
        {
            return;
        }

        var value = item.Directory;

        var networkTracksPath = Path.Join(value.FullName, "RailNetwork");
        var tracksPath = Path.Join(value.FullName, "Track");

        var networkBinaries = GetTrackBinaryPaths(networkTracksPath);
        var trackBinaries = GetTrackBinaryPaths(tracksPath);

        var blueprints = new List<Blueprint>();

        var set = networkBinaries
            .Concat(trackBinaries)
            .Select(path =>
            {
                var blueprintId = path
                    .Replace(value.FullName, string.Empty)
                    .TrimStart('/')
                    .Replace('/', '\\')
                    .Replace(".bin", ".xml");

                return new Blueprint
                {
                    BlueprintId = blueprintId,
                    BlueprintSetIdProduct = SelectedProduct.Name,
                    BlueprintSetIdProvider = SelectedProvider.Name,
                };
            })
            .ToHashSet();

        blueprints.AddRange(set);

        var archives = Directory.EnumerateFiles(value.FullName, "*.ap", SearchOption.TopDirectoryOnly);

        foreach (var archive in archives)
        {
            var networkFiles = Archives.ListFilesInPath(archive, "RailNetwork", ".bin");
            var trackFiles = Archives.ListFilesInPath(archive, "Track", ".bin");

            var binaries = networkFiles.Concat(trackFiles).Select(file => new Blueprint
            {
                BlueprintId = file.Replace(".XSec", ".xml"),
                BlueprintSetIdProduct = SelectedProduct.Name,
                BlueprintSetIdProvider = SelectedProvider.Name,
            });

            blueprints.AddRange(binaries);
        }

        var tracks = new List<Track>();

        foreach (var blueprint in blueprints)
        {
            var document = blueprint.GetBlueprintXmlInternal();
            var displayName = document.SelectLocalisedStringContent("cTrackSectionBlueprint DisplayName");
            var name = document.SelectTextContent("Name");

            var track = new Track
            {
                Blueprint = blueprint,
                Name = string.IsNullOrEmpty(displayName) ? name : displayName,
            };

            tracks.Add(track);
        }

        var sorted = tracks.OrderBy(t => t.Name);

        Tracks = new ObservableCollection<Track>(sorted);
    }

    private static List<string> GetTrackBinaryPaths(string path)
    {
        if (Paths.Exists(path) is false) return [];

        var xsecs = Directory.EnumerateFiles(path, "*.XSec", SearchOption.AllDirectories);

        var directories = xsecs
            .Select(Path.GetDirectoryName)
            .Where(x => string.IsNullOrEmpty(x) is false)!
            .ToHashSet<string>();

        return directories
            .SelectMany(d => Directory.EnumerateFiles(d, "*.bin", SearchOption.TopDirectoryOnly))
            .ToList();
    }
}
