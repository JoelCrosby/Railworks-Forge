using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

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

    public ReactiveCommand<Unit, Unit> LoadAvailableStockCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }

    public ReactiveCommand<Unit, Unit> AddVehicleCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteVehicleCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceVehicleCommand { get; }

    [ObservableProperty]
    private ObservableCollection<RollingStockEntry> _availableStock;

    [ObservableProperty]
    private BrowserDirectory? _selectedDirectory;

    [ObservableProperty]
    private RollingStockEntry? _selectedVehicle;

    [ObservableProperty]
    private ConsistRailVehicle? _selectedConsistVehicle;

    [ObservableProperty]
    private List<ConsistRailVehicle> _selectedConsistVehicles;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _loadAvailableStockProgress;

    [ObservableProperty]
    private ObservableCollection<BrowserDirectory> _directoryTree;

    public ObservableCollection<ConsistRailVehicle> RailVehicles { get; }

    public ConsistDetailViewModel(Scenario scenario, Consist consist, AssetDirectoryTreeService directoryTreeService)
    {
        _scenario = scenario;
        _consist = consist;

        AvailableStock = [];
        SelectedConsistVehicles = [];

        DirectoryTree = directoryTreeService.GetDirectoryTree();

        IsLoading = true;

        LoadAvailableStockCommand = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(LoadAvailableStock));
        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedDirectory is null) return;

            Launcher.Open(SelectedDirectory.AssetDirectory.Path);
        });

        RailVehicles = [];

        AddVehicleCommand = ReactiveCommand.CreateFromTask(AddVehicle);
        DeleteVehicleCommand = ReactiveCommand.CreateFromTask(DeleteVehicle);
        ReplaceVehicleCommand = ReactiveCommand.CreateFromTask(ReplaceVehicle);

        Refresh();
    }

    private void Refresh()
    {
        Observable.StartAsync(GetRailVehicles, RxApp.TaskpoolScheduler);
    }

    private async Task LoadAvailableStock()
    {
        if (SelectedDirectory?.AssetDirectory is null or not ProductDirectory)
        {
            return;
        }


        var binFiles = Directory
            .EnumerateFiles(SelectedDirectory.AssetDirectory.Path, "*.bin", SearchOption.AllDirectories)
            .Where(path =>
            {
                if (Utilities.RollingStockFolders.Any(path.Contains))
                {
                    return true;
                }

                return path.Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase) is not true;
            })
            .ToList();

        Dispatcher.UIThread.Post(() => AvailableStock.Clear());

        await ProcessBinaries(binFiles);
        await ProcessArchives();
    }

    private async Task ProcessArchives()
    {
        if (SelectedDirectory?.AssetDirectory is null or not ProductDirectory)
        {
            return;
        }

        var packages = Directory
            .EnumerateFiles(SelectedDirectory.AssetDirectory.Path, "*.ap", SearchOption.AllDirectories);

        var binFiles = new List<string>();

        foreach (var package in packages)
        {
            var archiveBinaries = Archives.ExtractFilesOfType(package, ".bin");
            binFiles.AddRange(archiveBinaries);
        }

        await ProcessBinaries(binFiles);
    }

    private async Task ProcessBinaries(List<string> binFiles)
    {
        LoadAvailableStockProgress = 0;

        var processedCount = 0;
        var processed = binFiles.Count;

        await Parallel.ForEachAsync(binFiles, async (binFile, cancellationToken) =>
        {
            var exported = await Serz.Convert(binFile, cancellationToken);
            var models = await GetConsistBlueprint(exported.OutputPath, cancellationToken);

            processedCount++;

            LoadAvailableStockProgress = (int) Math.Ceiling((double)(100 * processedCount) / processed);

            Dispatcher.UIThread.Post(() => AvailableStock.AddRange(models));
        });
    }

    private async Task GetRailVehicles()
    {
        if (string.IsNullOrWhiteSpace(_consist.BlueprintId))
        {
            return;
        }

        var consists = await _scenario.GetServiceConsistVehicles(_consist.Id);

        Dispatcher.UIThread.Post(() => IsLoading = false);

        Dispatcher.UIThread.Post(() =>
        {
            RailVehicles.Clear();
            RailVehicles.AddRange(consists);
        });
    }

    private static async Task<List<RollingStockEntry>> GetConsistBlueprint(string path, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        var doc = await XmlParser.ParseDocumentAsync(text, cancellationToken);
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

        await runner.Run();

        Refresh();
    }

    private async Task ReplaceVehicle()
    {
        if (SelectedVehicle is null || SelectedConsistVehicles.Any() is false)
        {
            return;
        }

        var replacements = SelectedConsistVehicles.ConvertAll(target => new VehicleReplacement
        {
            Replacement = SelectedVehicle,
            Target = target,
        });

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

        await runner.Run();

        Refresh();
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

        await runner.Run();

        Refresh();
    }
}
