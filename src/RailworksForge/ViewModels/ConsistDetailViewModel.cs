using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ConsistDetailViewModel : ViewModelBase
{
    private readonly Scenario _scenario;
    private readonly Consist _consist;

    [ObservableProperty]
    private FileBrowserViewModel _fileBrowser;

    [ObservableProperty]
    private ObservableCollection<PreloadConsist> _availableStock;

    public IObservable<ObservableCollection<ConsistRailVehicle>> RailVehicles { get; }

    public ConsistDetailViewModel(Scenario scenario, Consist consist)
    {
        _scenario = scenario;
        _consist = consist;

        AvailableStock = [];
        RailVehicles = Observable.FromAsync(GetRailVehicles, RxApp.TaskpoolScheduler);
        FileBrowser = new FileBrowserViewModel(Paths.GetAssetsDirectory());
    }

    public async Task LoadAvailableStock(BrowserDirectory directory)
    {
        var preloadDirectory = Path.Join(directory.FullPath, "PreLoad");

        if (!Directory.Exists(preloadDirectory))
        {
            return;
        }

        var binFiles = Directory.EnumerateFiles(preloadDirectory, "*.bin", SearchOption.AllDirectories);

        var items = new List<PreloadConsist>();

        foreach (var binFile in binFiles)
        {
            var exported = await Serz.Convert(binFile);
            var consists = await GetConsistBlueprints(exported.OutputPath);

            items.AddRange(consists);
        }

        AvailableStock = new ObservableCollection<PreloadConsist>(items);
    }

    private async Task<ObservableCollection<ConsistRailVehicle>> GetRailVehicles()
    {
        if (string.IsNullOrWhiteSpace(_consist.BlueprintId))
        {
            return [];
        }

        var consists = await _scenario.GetConsists(_consist.ServiceId);

        return new ObservableCollection<ConsistRailVehicle>(consists);
    }

    private static async Task<List<PreloadConsist>> GetConsistBlueprints(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        var doc = await XmlParser.ParseDocumentAsync(text);

        return doc
            .QuerySelectorAll("Blueprint")
            .Select(PreloadConsist.Parse)
            .ToList();
    }
}
