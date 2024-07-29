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

    [ObservableProperty]
    private ObservableCollection<PreloadConsistViewModel> _availableStock;

    [ObservableProperty]
    private BrowserDirectory? _selectedDirectory;

    [ObservableProperty]
    private bool _isLoading;

    public IObservable<ObservableCollection<ConsistRailVehicle>> RailVehicles { get; }

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
            .Where(f => f.Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase) is not true);

        await Parallel.ForEachAsync(binFiles, async (binFile, cancellationToken) =>
        {
            var exported = await Serz.Convert(binFile, false, cancellationToken);
            var consists = await GetConsistBlueprint(exported.OutputPath, cancellationToken);
            var models = consists.ConvertAll(c => new PreloadConsistViewModel(c));

            Dispatcher.UIThread.Post(() => AvailableStock.AddRange(models));
        });
    }

    private async Task<ObservableCollection<ConsistRailVehicle>> GetRailVehicles()
    {
        if (string.IsNullOrWhiteSpace(_consist.BlueprintId))
        {
            return [];
        }

        var consists = await _scenario.GetServiceConsistVehicles(_consist.ServiceId);

        Dispatcher.UIThread.Post(() => IsLoading = false);

        return new ObservableCollection<ConsistRailVehicle>(consists);
    }

    private static async Task<List<PreloadConsist>> GetConsistBlueprint(string path, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        var doc = await XmlParser.ParseDocumentAsync(text, cancellationToken);

        return doc
            .QuerySelectorAll("Blueprint")
            .Select(PreloadConsist.Parse)
            .ToList();
    }
}
