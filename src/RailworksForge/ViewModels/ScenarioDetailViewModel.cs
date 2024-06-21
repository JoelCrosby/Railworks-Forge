using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class ScenarioDetailViewModel : ViewModelBase
{
    public Scenario Scenario { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, string> ExportBinXmlCommand { get; }
    public ReactiveCommand<Unit, string> ExportXmlBinCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractScenariosCommand { get; }
    public ReactiveCommand<Unit, Unit> ClickedConsistCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteConsistCommand { get; }

    public Consist? SelectedConsist { get; set; }

    public ScenarioDetailViewModel(Scenario scenario)
    {
        Scenario = scenario;

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (Scenario.DirectoryPath is null) return;

            Launcher.Open(Scenario.DirectoryPath);
        });

        ExportBinXmlCommand = ReactiveCommand.CreateFromTask(scenario.ConvertBinToXml);
        ExportXmlBinCommand = ReactiveCommand.CreateFromTask(scenario.ConvertXmlToBin);

        ExtractScenariosCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            return Observable.Start(scenario.Route.ExtractScenarios);
        });

        ClickedConsistCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedConsist is null) return;

            Utils.GetApplicationViewModel().SelectScenarioConsist(Scenario, SelectedConsist);
        });

        SaveConsistCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedConsist is null)
            {
                return;
            }

            var consistElement = await GetSavedConsistRailVehicleElement();

            var result = await Utils.GetApplicationViewModel().ShowSaveConsistDialog.Handle(new SaveConsistViewModel
            {
                ConsistElement = consistElement,
                Name = SelectedConsist.LocomotiveName,
                LocomotiveName = SelectedConsist.LocomotiveName,
            });

            if (result?.Name is null) return;

            PersistenceService.SaveConsist(result with
            {
                ConsistElement = consistElement,
            });
        });

        ReplaceConsistCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedConsist is null)
            {
                return;
            }

            var result = await Utils.GetApplicationViewModel().ShowReplaceConsistDialog.Handle(new ReplaceConsistViewModel
            {
                AvailableStock = [],
                TargetConsist = SelectedConsist,
                Scenario = Scenario,
            });

            if (result is null) return;

            await ConsistService.ReplaceConsist(SelectedConsist, result, Scenario);
        });

        DeleteConsistCommand = ReactiveCommand.Create(() => {});
    }

    private async Task<string> GetSavedConsistRailVehicleElement()
    {
        ArgumentNullException.ThrowIfNull(SelectedConsist);

        var doc = await Scenario.GetXmlDocument();

        var vehicles = doc
            .QuerySelectorAll("cConsist")
            .FirstOrDefault(el => el.SelectTextContnet("ServiceName Key") == SelectedConsist.ServiceId)?
            .QuerySelector("RailVehicles");

        if (vehicles is null)
        {
            throw new Exception("could not find consist in scenario bin");
        }

        return vehicles.OuterHtml;
    }
}
