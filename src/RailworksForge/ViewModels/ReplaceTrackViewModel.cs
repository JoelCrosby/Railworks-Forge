using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AngleSharp.Xml.Dom;

using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
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

    public ReactiveCommand<Unit, ReplaceTracksRequest> ReplaceTracksCommand { get; }

    public IObservable<List<SelectTrackViewModel>> RouteTracks { get; init; }

    public ReplaceTrackViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        ReplaceTracksCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (RouteTracks is null)
            {
                ArgumentNullException.ThrowIfNull(RouteTracks);
            }

            var tracks = await RouteTracks.FirstAsync();

            return new ReplaceTracksRequest
            {
                Replacements = tracks.ConvertAll(r => new TrackReplacement
                {
                    Blueprint = r.RouteBlueprint,
                    ReplacementBlueprint = r.SelectedTrack,
                }),
            };
        });

        RouteTracks = Observable.Return(new List<SelectTrackViewModel>());

        if (Design.IsDesignMode is false)
        {
            RouteTracks = Observable.FromAsync(GetRouteTracks, RxApp.TaskpoolScheduler);
        }
    }

    private async Task<List<SelectTrackViewModel>> GetRouteTracks()
    {
        var document = await GetRouteTracksDocument();

        if (document is null)
        {
            throw new Exception("could not read route tracks file");
        }

        var models = await Task.Run(() =>
        {
            var blueprints = document
                .QuerySelectorAll("Network-cSectionGenericProperties BlueprintID")
                .Select(element =>
                {
                    var provider = element.SelectTextContent("Provider");
                    var product = element.SelectTextContent("Product");
                    var blueprintId = element.SelectTextContent("BlueprintID");

                    return new Blueprint
                    {
                        BlueprintId = blueprintId,
                        BlueprintSetIdProduct = product,
                        BlueprintSetIdProvider = provider,
                    };
                });

            return blueprints
                .ToHashSet()
                .Where(blueprint => string.IsNullOrEmpty(blueprint.BlueprintId) is false)
                .Select(track => new SelectTrackViewModel { RouteBlueprint = track })
                .ToList();
        });

        Dispatcher.UIThread.Post(() => IsLoading = false);

        return models;
    }

    private async Task<IXmlDocument?> GetRouteTracksDocument()
    {
        var trackBinPath = Path.Join(Route.DirectoryPath, "Networks", "Tracks.bin");

        if (Paths.Exists(trackBinPath))
        {
            var output = await Serz.Convert(trackBinPath);
            var xml = await File.ReadAllTextAsync(output.OutputPath);

            return await XmlParser.ParseDocumentAsync(xml);
        }

        var archivePath = Route.MainContentArchivePath;
        var destination = Paths.GetAssetCachePath(trackBinPath, false);

        Archives.ExtractFileContentFromPath(archivePath, "Networks/Tracks.bin", destination);

        var compressedOutput = await Serz.Convert(destination);

        if (Paths.Exists(compressedOutput.OutputPath))
        {
            var xml = await File.ReadAllTextAsync(compressedOutput.OutputPath);
            return await XmlParser.ParseDocumentAsync(xml);
        }

        return null;
    }
}

public partial class SelectTrackViewModel : ViewModelBase
{
    public static List<DirectoryInfo> Providers => Paths.GetAssetProviders();

    [ObservableProperty]
    private ObservableCollection<DirectoryInfo> _products = [];

    [ObservableProperty]
    private ObservableCollection<Blueprint> _tracks = [];

    [ObservableProperty]
    private DirectoryInfo? _selectedProvider;

    [ObservableProperty]
    private DirectoryInfo? _selectedProduct;

    [ObservableProperty]
    private Blueprint? _selectedTrack;

    public required Blueprint RouteBlueprint { get; init; }

    partial void OnSelectedProviderChanged(DirectoryInfo? value)
    {
        if (value is null) return;

        var products = Paths.GetAssetProviderProducts(value.Name);

        Products = new ObservableCollection<DirectoryInfo>(products);
    }

    partial void OnSelectedProductChanged(DirectoryInfo? value)
    {
        if (value is null || SelectedProvider is null || SelectedProduct is null)
        {
            return;
        }

        var networkTracksPath = Path.Join(value.FullName, "RailNetwork");
        var tracksPath = Path.Join(value.FullName, "Track");

        var networkBinaries = Paths.Exists(networkTracksPath) ? Directory.EnumerateFiles(networkTracksPath, "*.XSec", SearchOption.AllDirectories) : [];
        var trackBinaries = Paths.Exists(tracksPath) ? Directory.EnumerateFiles(tracksPath, "*.XSec", SearchOption.AllDirectories) : [];

        var tracks = new List<Blueprint>();

        var set = networkBinaries
            .Concat(trackBinaries)
            .Select(path =>
            {
                var blueprintId = path.Replace(value.FullName, string.Empty).Replace(".XSec", ".xml");

                return new Blueprint
                {
                    BlueprintId = blueprintId,
                    BlueprintSetIdProduct = SelectedProduct.Name,
                    BlueprintSetIdProvider = SelectedProvider.Name,
                };
            })
            .ToHashSet();

        tracks.AddRange(set);

        var archives = Directory.EnumerateFiles(value.FullName, "*.ap", SearchOption.TopDirectoryOnly);

        foreach (var archive in archives)
        {
            var networkFiles = Archives.ListFilesInPath(archive, "RailNetwork", ".XSec");
            var trackFiles = Archives.ListFilesInPath(archive, "Track", ".XSec");

            var binaries = networkFiles.Concat(trackFiles).Select(file => new Blueprint
            {
                BlueprintId = file.Replace(".XSec", ".xml"),
                BlueprintSetIdProduct = SelectedProduct.Name,
                BlueprintSetIdProvider = SelectedProvider.Name,
            });

            tracks.AddRange(binaries);
        }

        Tracks = new ObservableCollection<Blueprint>(tracks);
    }
}
