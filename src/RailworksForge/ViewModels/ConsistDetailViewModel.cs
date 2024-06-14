using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public partial class ConsistDetailViewModel : ViewModelBase
{
    public Scenario Scenario { get; }
    public Consist Consist { get; }

    [ObservableProperty]
    public FileBrowserViewModel _fileBrowser;

    [ObservableProperty]
    public ObservableCollection<ConsistRailVehicle> _AvailableStock;

    public Task<ObservableCollection<ConsistRailVehicle>> RailVehicles { get; }

    public ConsistDetailViewModel(Scenario scenario, Consist consist)
    {
        Scenario = scenario;
        Consist = consist;

        RailVehicles = GetRailVehicles();
        FileBrowser = new FileBrowserViewModel(Paths.GetAssetsDirectory());
    }

    private async Task<ObservableCollection<ConsistRailVehicle>> GetRailVehicles()
    {
        var path = await Scenario.ConvertBinToXml();
        var text = await File.ReadAllTextAsync(path);

        var doc = new HtmlParser().ParseDocument(text);
        var consists = doc
            .QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("BlueprintID BlueprintID") == Consist.BlueprintId)?
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
