using System;
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
using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ConsistDetailViewModel : ViewModelBase
{
    public Scenario Scenario { get; }
    public Consist Consist { get; }

    [ObservableProperty]
    public FileBrowserViewModel _fileBrowser;

    [ObservableProperty]
    public ObservableCollection<ConsistRailVehicle> _AvailableStock;

    public IObservable<ObservableCollection<ConsistRailVehicle>> RailVehicles { get; }

    public ConsistDetailViewModel(Scenario scenario, Consist consist)
    {
        Scenario = scenario;
        Consist = consist;

        AvailableStock = [];
        RailVehicles = Observable.FromAsync(GetRailVehicles, RxApp.TaskpoolScheduler);
        FileBrowser = new FileBrowserViewModel(Paths.GetAssetsDirectory());
    }

    private async Task<ObservableCollection<ConsistRailVehicle>> GetRailVehicles()
    {
        if (string.IsNullOrWhiteSpace(Consist.BlueprintId))
        {
            return [];
        }

        var path = await Scenario.ConvertBinToXml();
        var text = await File.ReadAllTextAsync(path);
        var doc = await new HtmlParser().ParseDocumentAsync(text);

        var consists = doc
            .QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("ServiceName Key") == Consist.ServiceId)?
            .QuerySelectorAll("RailVehicles cOwnedEntity")
            .Select(ParseConsist)
            .ToList() ?? [];

        return new ObservableCollection<ConsistRailVehicle>(consists);
    }

    private static ConsistRailVehicle ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContnet("Name");
        var uniqueNumber = el.SelectTextContnet("UniqueNumber");
        var flipped = el.SelectTextContnet("Flipped") == "1";

        return new ConsistRailVehicle
        {
            Id = consistId,
            LocomotiveName = locomotiveName,
            UniqueNumber = uniqueNumber,
            Flipped = flipped,
        };
    }
}
