using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;

namespace RailworksForge.ViewModels;

public class ConsistViewModel : ViewModelBase
{
    public Scenario Scenario { get; }
    public Consist Consist { get; }

    public Task<ObservableCollection<ConsistRailVehicle>> RailVehicles { get; }

    public ConsistViewModel(Scenario scenario, Consist consist)
    {
        Scenario = scenario;
        Consist = consist;

        RailVehicles = GetRailVehicles();
    }

    private async Task<ObservableCollection<ConsistRailVehicle>> GetRailVehicles()
    {
        var path = await Scenario.ConvertBinToXml();
        var text = await File.ReadAllTextAsync(path);

        var doc = new HtmlParser().ParseDocument(text);
        var consists = doc
            .QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.Attributes.Any(attr => attr.Name == Consist.Id))?
            .QuerySelectorAll("RailVehicles")
            .Select(ParseConsist)
            .ToList() ?? [];

        return new ObservableCollection<ConsistRailVehicle>(consists);
    }

    private static ConsistRailVehicle ParseConsist(IElement el)
    {
        var consistId = el.GetAttribute("d:id") ?? string.Empty;
        var locomotiveName = el.SelectTextContnet("LocoName English");

        return new ConsistRailVehicle
        {
            Id = consistId,
            LocomotiveName = locomotiveName,
        };
    }
}
