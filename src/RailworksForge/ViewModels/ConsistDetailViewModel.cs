using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
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
    private ObservableCollection<ConsistBlueprint> _availableStock;

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

        var items = new List<ConsistBlueprint>();

        foreach (var binFile in binFiles)
        {
            var exported = await Serz.Convert(binFile);
            var consists = await GetConsistBlueprints(exported.OutputPath);

            items.AddRange(consists);
        }

        AvailableStock = new ObservableCollection<ConsistBlueprint>(items);
    }

    private async Task<ObservableCollection<ConsistRailVehicle>> GetRailVehicles()
    {
        if (string.IsNullOrWhiteSpace(_consist.BlueprintId))
        {
            return [];
        }

        var path = await _scenario.ConvertBinToXml();
        var consists = await GetConsists(path);

        return new ObservableCollection<ConsistRailVehicle>(consists);
    }

    private async Task<List<ConsistRailVehicle>> GetConsists(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        var doc = await new HtmlParser().ParseDocumentAsync(text);

        return doc
            .QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("ServiceName Key") == _consist.ServiceId)?
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .Select(ParseConsist)
            .ToList() ?? [];
    }

    private static async Task<List<ConsistBlueprint>> GetConsistBlueprints(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        var doc = await new HtmlParser().ParseDocumentAsync(text);

        return doc
            .QuerySelectorAll("Blueprint")
            .Select(ParseConsistBlueprint)
            .ToList();
    }

    private static ConsistRailVehicle ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContnet("Name");
        var uniqueNumber = el.SelectTextContnet("UniqueNumber");
        var blueprintId = el.SelectTextContnet("BlueprintID BlueprintID");
        var flipped = el.SelectTextContnet("Flipped") == "1";
        var blueprintSetIdProduct = el.SelectTextContnet("iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContnet("iBlueprintLibrary-cBlueprintSetID Provider");

        return new ConsistRailVehicle
        {
            Id = consistId,
            LocomotiveName = locomotiveName,
            UniqueNumber = uniqueNumber,
            Flipped = flipped,
            BlueprintId = blueprintId,
            BlueprintSetIdProduct = blueprintSetIdProduct,
            BlueprintSetIdProvider = blueprintSetIdProvider,
        };
    }

    private static ConsistBlueprint ParseConsistBlueprint(IElement el)
    {
        var locomotiveName = el.SelectTextContnet("LocoName English");
        var displayName = el.SelectTextContnet("DisplayName English");

        return new ConsistBlueprint
        {
            LocomotiveName = locomotiveName,
            DisplayName = displayName,
        };
    }
}
