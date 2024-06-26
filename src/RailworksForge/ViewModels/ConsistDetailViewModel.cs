using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AngleSharp.Dom;

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

        var path = await _scenario.ConvertBinToXml();
        var consists = await GetConsists(path);

        return new ObservableCollection<ConsistRailVehicle>(consists);
    }

    private async Task<List<ConsistRailVehicle>> GetConsists(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        var doc = await XmlParser.ParseDocumentAsync(text);

        return doc
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("ServiceName Key", _consist.ServiceId)?
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .Select(ParseConsist)
            .ToList() ?? [];
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

    private static ConsistRailVehicle ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContent("Name");
        var uniqueNumber = el.SelectTextContent("UniqueNumber");
        var blueprintId = el.SelectTextContent("BlueprintID BlueprintID");
        var flipped = el.SelectTextContent("Flipped") == "1";
        var blueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Product");
        var blueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cBlueprintSetID Provider");

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
}
