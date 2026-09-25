using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
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
    private List<SelectTrackViewModel> _routeTracks;

    public ReactiveCommand<Unit, ReplaceTracksRequest> ReplaceTracksCommand { get; }

    public bool CanReplace
    {
        get
        {
            var isReady = !Loading.IsLoading && !Loading.HasError;
            var tracksReady = RouteTracks.All(track => !track.Loading.IsLoading && !track.Loading.HasError);
            var hasSelection = RouteTracks.Any(track => track.SelectedTrack is not null);

            return isReady && tracksReady && hasSelection;
        }
    }

    partial void OnRouteTracksChanged(List<SelectTrackViewModel> value)
    {
        foreach (var track in value)
        {
            track.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CanReplace));
            track.Loading.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CanReplace));
        }

        OnPropertyChanged(nameof(CanReplace));
    }

    public ReplaceTrackViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        RouteTracks = [];
        Loading.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CanReplace));

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
            _ = Loading.RunAsync("Loading route tracks…", _ => GetRouteTracks(), tracks => RouteTracks = tracks);
        }
    }

    public override void CancelLoading()
    {
        base.CancelLoading();

        foreach (var track in RouteTracks)
        {
            track.CancelLoading();
        }
    }

    private async Task<List<SelectTrackViewModel>> GetRouteTracks()
    {
        var blueprints = await Route.GetTrackBlueprints();
        var providers = Paths.GetAssetProviders().ConvertAll(directory => new DirectoryItem(directory.Name, directory));

        var  models = blueprints
            .Select(blueprint => new SelectTrackViewModel
            {
                Providers = providers,
                RouteBlueprint = blueprint.Blueprint,
                TrackCount = blueprint.Count,
            })
            .ToList();

        return models;
    }
}

public record DirectoryItem(string Name, DirectoryInfo Directory)
{
    public override string ToString() => Name;
}

public partial class SelectTrackViewModel : ViewModelBase
{
    public List<DirectoryItem> Providers { get; init; } = [];

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
            var products = Paths.GetAssetProviderProducts(value.Name);
            var items = products.ConvertAll(product => new DirectoryItem(product.Name, product));

            return Task.FromResult(items);
        }, items => Products = new ObservableCollection<DirectoryItem>(items));
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

        _ = Loading.RunAsync("Loading track blueprints…",
            token => GetTracks(provider, value, token),
            tracks => Tracks = new ObservableCollection<Track>(tracks));
    }

    private static async Task<List<Track>> GetTracks(
        DirectoryItem provider,
        DirectoryItem value,
        CancellationToken token)
    {
        var directory = value.Directory;

        var networkTracksPath = Path.Join(directory.FullName, "RailNetwork");
        var tracksPath = Path.Join(directory.FullName, "Track");

        var networkBinaries = GetTrackBinaryPaths(networkTracksPath);
        var trackBinaries = GetTrackBinaryPaths(tracksPath);

        var blueprints = new List<Blueprint>();

        var set = networkBinaries
            .Concat(trackBinaries)
            .Select(path =>
            {
                var blueprintId = path
                    .Replace(directory.FullName, string.Empty)
                    .TrimStart('/')
                    .Replace('/', '\\')
                    .Replace(".bin", ".xml");

                return new Blueprint
                {
                    BlueprintId = blueprintId,
                    BlueprintSetIdProduct = value.Name,
                    BlueprintSetIdProvider = provider.Name,
                };
            })
            .ToHashSet();

        blueprints.AddRange(set);

        var archives = Directory.EnumerateFiles(directory.FullName, "*.ap", SearchOption.TopDirectoryOnly);

        foreach (var archive in archives)
        {
            var networkFiles = Archives.ListFilesInPath(archive, "RailNetwork", ".bin");
            var trackFiles = Archives.ListFilesInPath(archive, "Track", ".bin");

            var binaries = networkFiles.Concat(trackFiles).Select(file => new Blueprint
            {
                BlueprintId = file.Replace(".XSec", ".xml"),
                BlueprintSetIdProduct = value.Name,
                BlueprintSetIdProvider = provider.Name,
            });

            blueprints.AddRange(binaries);
        }

        var tracks = new List<Track>();

        foreach (var blueprint in blueprints)
        {
            token.ThrowIfCancellationRequested();
            using var document = await blueprint.GetXmlDocument();
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


        return sorted.ToList();
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
