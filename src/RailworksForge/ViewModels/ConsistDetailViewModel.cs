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
using RailworksForge.Core.Commands;
using RailworksForge.Core.Commands.Common;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ConsistDetailViewModel : ViewModelBase
{
    private readonly Scenario _scenario;
    private readonly Consist _consist;
    private readonly AssetDirectoryTreeService _directoryTreeService;

    public LoadingOperation StockLoading { get; } = new();

    public ReactiveCommand<Unit, Unit> LoadAvailableStockCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenConsistVehicleInExplorerCommand { get; }

    public ReactiveCommand<Unit, Unit> AddVehicleCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteVehicleCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceVehicleCommand { get; }

    [ObservableProperty]
    private BrowserDirectory? _selectedDirectory;

    [ObservableProperty]
    private RollingStockEntry? _selectedVehicle;

    public IReadOnlyList<ConsistRailVehicle> SelectedConsistVehicles { get; set; } = [];

    private ConsistRailVehicle? SelectedConsistVehicle => SelectedConsistVehicles.Count is 1 ? SelectedConsistVehicles[0] : null;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private string? _directoryTreeSearchTerm;

    private List<ConsistRailVehicle> _cachedRailVehicles = [];

    [ObservableProperty]
    private ObservableCollection<BrowserDirectory> _directoryTree;

    public ObservableCollection<ConsistRailVehicle> RailVehicles { get; }

    public ObservableCollection<RollingStockEntry> AvailableStock { get; }

    public ConsistDetailViewModel(Scenario scenario, Consist consist, AssetDirectoryTreeService directoryTreeService)
    {
        _scenario = scenario;
        _consist = consist;

        IsLoading = true;
        AvailableStock = [];
        RailVehicles = [];

        _directoryTreeService = directoryTreeService;
        DirectoryTree = [];

        LoadAvailableStockCommand = ReactiveCommand.CreateFromTask(LoadAvailableStock);

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedDirectory is null) return;

            Launcher.Open(SelectedDirectory.AssetDirectory.Path);
        });

        OpenConsistVehicleInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedConsistVehicle?.BinaryDirectoryPath is null) return;

            Launcher.Open(SelectedConsistVehicle.BinaryDirectoryPath);
        });

        AddVehicleCommand = ReactiveCommand.CreateFromTask(AddVehicle);
        DeleteVehicleCommand = ReactiveCommand.CreateFromTask(DeleteVehicle);
        ReplaceVehicleCommand = ReactiveCommand.CreateFromTask(ReplaceVehicle);

        this.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SearchTerm))
            {
                var invariant = _searchTerm?.ToLowerInvariant();
                var indexed = invariant is null ? _cachedRailVehicles : _cachedRailVehicles.Where(x => x.SearchIndex.Contains(invariant));

                RailVehicles.Clear();
                RailVehicles.AddRange(indexed);
            }

            if (e.PropertyName is nameof(DirectoryTreeSearchTerm))
            {
                var invariant = _directoryTreeSearchTerm?.ToLowerInvariant();

                var indexed = string.IsNullOrWhiteSpace(invariant)
                    ? directoryTreeService.GetDirectoryTree()
                    : directoryTreeService.GetDirectoryTree()
                        .Where(x => x.SearchIndex.Contains(invariant) || x.Subfolders.Any(y => y.SearchIndex.Contains(invariant)));

                DirectoryTree = [..indexed];
            }
        };

        Refresh();
    }

    public override void CancelLoading()
    {
        base.CancelLoading();
        StockLoading.Cancel();
    }

    partial void OnSelectedDirectoryChanged(BrowserDirectory? value)
    {
        StockLoading.Cancel();
        AvailableStock.Clear();
        SelectedVehicle = null;
    }

    private void Refresh()
    {
        _ = Loading.RunAsync("Loading consist vehicles…", async token =>
        {
            Cache.BlueprintAcquisitionStates.Clear();
            Cache.ArchiveCache.Clear();
            await _directoryTreeService.LoadDirectoryTree();
            token.ThrowIfCancellationRequested();
            var directories = _directoryTreeService.GetDirectoryTree().ToList();
            var vehicles = string.IsNullOrWhiteSpace(_consist.BlueprintId)
                ? []
                : await _scenario.GetServiceConsistVehicles(_consist);

            foreach (var vehicle in vehicles)
            {
                token.ThrowIfCancellationRequested();
                _ = vehicle.AcquisitionState;
            }

            return (Directories: directories, Vehicles: vehicles);
        }, result =>
        {
            DirectoryTree = [..result.Directories];
            _cachedRailVehicles = result.Vehicles;
            RailVehicles.Clear();
            RailVehicles.AddRange(result.Vehicles);
        });
    }

    private Task LoadAvailableStock()
    {
        var directory = SelectedDirectory?.AssetDirectory;

        if (directory is not ProductDirectory)
        {
            return Task.CompletedTask;
        }

        AvailableStock.Clear();

        return StockLoading.RunAsync("Loading available rolling stock…", async token =>
        {
            var binFiles = Directory.EnumerateFiles(directory.Path, "*.bin", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var package in Directory.EnumerateFiles(directory.Path, "*.ap", SearchOption.AllDirectories))
            {
                token.ThrowIfCancellationRequested();
                binFiles.AddRange(Archives.ExtractFilesOfType(package, ".bin"));
            }

            var results = new System.Collections.Concurrent.ConcurrentBag<RollingStockEntry>();
            var options = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = 4 };
            await Parallel.ForEachAsync(binFiles, options, async (binFile, cancellationToken) =>
            {
                var exported = await Serz.Convert(binFile, cancellationToken);
                var models = await GetConsistBlueprint(exported.OutputPath, cancellationToken);

                foreach (var model in models)
                {
                    results.Add(model);
                }
            });

            return results.OrderBy(model => model.DisplayName).ToList();
        }, models => AvailableStock.AddRange(models));
    }

    private static async Task<List<RollingStockEntry>> GetConsistBlueprint(string path, CancellationToken cancellationToken)
    {
        using var text = File.OpenRead(path);
        using var doc = await XmlParser.ParseDocumentAsync(text, cancellationToken);
        var blueprint = Blueprint.FromPath(path);

        return doc
            .QuerySelectorAll("Blueprint")
            .Select(el => RollingStockEntry.Parse(el, blueprint))
            .Where(e => e.BlueprintType is BlueprintType.Engine or BlueprintType.Tender or BlueprintType.Wagon)
            .ToList();
    }

    private async Task AddVehicle()
    {
        if (SelectedVehicle is null) return;

        var request = new AddConsistVehicleRequest
        {
            VehicleToAdd = SelectedVehicle,
        };

        var runner = new ConsistCommandRunner
        {
            Scenario = _scenario,
            Commands = [new AddConsistVehicle(request)],
        };

        await Loading.RunAsync("Updating consist…", _ => runner.Run());

        if (!Loading.HasError && IsActive)
        {
            Refresh();
        }
    }

    private async Task ReplaceVehicle()
    {
        if (SelectedVehicle is null || SelectedConsistVehicles.Any() is false)
        {
            return;
        }

        var replacements = SelectedConsistVehicles
            .Select(target => new VehicleReplacement
            {
                Replacement = SelectedVehicle,
                Target = target,
            })
            .ToList();

        var request = new ReplaceVehiclesRequest
        {
            Consist = _consist,
            Replacements = replacements,
        };

        var runner = new ConsistCommandRunner
        {
            Scenario = _scenario,
            Commands = [new ReplaceConsistVehicles(request)],
        };

        await Loading.RunAsync("Updating consist…", _ => runner.Run());

        if (!Loading.HasError && IsActive)
        {
            Refresh();
        }
    }

    private async Task DeleteVehicle()
    {
        if (SelectedConsistVehicle is null) return;

        var request = new DeleteConsistVehicleRequest
        {
            Consist = _consist,
            VehicleToDelete = SelectedConsistVehicle,
        };

        var runner = new ConsistCommandRunner
        {
            Scenario = _scenario,
            Commands = [new DeleteConsistVehicle(request)],
        };

        await Loading.RunAsync("Updating consist…", _ => runner.Run());

        if (!Loading.HasError && IsActive)
        {
            Refresh();
        }
    }
}
