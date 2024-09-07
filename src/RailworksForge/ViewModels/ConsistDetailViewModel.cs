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
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ConsistDetailViewModel : ViewModelBase
{
    private readonly Scenario _scenario;
    private readonly Consist _consist;

    public ReactiveCommand<Unit, Unit> LoadAvailableStockCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> AddToConsistCommand { get; }

    [ObservableProperty]
    private ObservableCollection<RollingStockEntry> _availableStock;

    [ObservableProperty]
    private BrowserDirectory? _selectedDirectory;

    [ObservableProperty]
    private RollingStockEntry? _selectedVehicle;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _loadAvailableStockProgress;

    public IObservable<List<ConsistRailVehicle>> RailVehicles { get; }

    public ObservableCollection<BrowserDirectory> DirectoryTree { get; }

    public ConsistDetailViewModel(Scenario scenario, Consist consist)
    {
        _scenario = scenario;
        _consist = consist;

        AvailableStock = [];
        DirectoryTree = new ObservableCollection<BrowserDirectory>(Paths.GetTopLevelRailVehicleDirectories());

        IsLoading = true;

        RailVehicles = Observable.FromAsync(GetRailVehicles, RxApp.TaskpoolScheduler);
        LoadAvailableStockCommand = ReactiveCommand.CreateFromTask(LoadAvailableStock);
        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedDirectory is null) return;

            Launcher.Open(SelectedDirectory.FullPath);
        });

        AddToConsistCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedVehicle is null) return;

            Console.WriteLine(SelectedVehicle.Blueprint.BlueprintId);
        });
    }

    private async Task LoadAvailableStock()
    {
        if (SelectedDirectory?.Level is not AssetBrowserLevel.Product)
        {
            return;
        }

        var railVehiclesDirectory = Path.Join(SelectedDirectory.FullPath, "RailVehicles");

        if (!Paths.Exists(railVehiclesDirectory))
        {
            return;
        }

        var binFiles = Directory
            .EnumerateFiles(railVehiclesDirectory, "*.bin", SearchOption.AllDirectories)
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

        LoadAvailableStockProgress = 0;

        var processedCount = 0;
        var processed = binFiles.Count;

        await Parallel.ForEachAsync(binFiles, async (binFile, cancellationToken) =>
        {
            var exported = await Serz.Convert(binFile, false, cancellationToken);
            var models = await GetConsistBlueprint(exported.OutputPath, cancellationToken);

            processedCount++;

            LoadAvailableStockProgress = (int) Math.Ceiling((double)(100 * processedCount) / processed);

            Dispatcher.UIThread.Post(() => AvailableStock.AddRange(models));
        });
    }

    private async Task<List<ConsistRailVehicle>> GetRailVehicles()
    {
        if (string.IsNullOrWhiteSpace(_consist.BlueprintId))
        {
            return [];
        }

        var consists = await _scenario.GetServiceConsistVehicles(_consist.ServiceId);

        Dispatcher.UIThread.Post(() => IsLoading = false);

        return consists;
    }

    private static async Task<List<RollingStockEntry>> GetConsistBlueprint(string path, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        var doc = await XmlParser.ParseDocumentAsync(text, cancellationToken);

        return doc
            .QuerySelectorAll("Blueprint")
            .Select(RollingStockEntry.Parse)
            .Where(e => e.BlueprintType is BlueprintType.Engine or BlueprintType.Tender or BlueprintType.Wagon)
            .ToList();
    }
}
