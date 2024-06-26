using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AngleSharp.Xml;

using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class ScenarioDetailViewModel : ViewModelBase
{
    public Scenario Scenario { get; }

    public FlatTreeDataGridSource<Consist> Source { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, string> ExportBinXmlCommand { get; }
    public ReactiveCommand<Unit, string> ExportXmlBinCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractScenariosCommand { get; }
    public ReactiveCommand<Unit, Unit> ClickedConsistCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteConsistCommand { get; }

    public IEnumerable<Consist> SelectedConsists { get; set; }

    private Consist? SelectedConsist => SelectedConsists.Count() is 1 ? SelectedConsists.First() : null;

    public ScenarioDetailViewModel(Scenario scenario)
    {
        Scenario = scenario;
        SelectedConsists = [];

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
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
            if (SelectedConsists.Any() is false)
            {
                return;
            }

            var result = await Utils.GetApplicationViewModel().ShowReplaceConsistDialog.Handle(new ReplaceConsistViewModel
            {
                AvailableStock = [],
                Scenario = Scenario,
            });

            if (result is null) return;

            var target = new TargetConsist(SelectedConsists);

            await ConsistService.ReplaceConsist(target, result, Scenario);
        });

        DeleteConsistCommand = ReactiveCommand.Create(() => {});

        Source = new FlatTreeDataGridSource<Consist>(scenario.Consists)
        {
            Columns =
            {
                new TextColumn<Consist, AcquisitionState>("State", x => x.AcquisitionState),
                new TextColumn<Consist, string>("Locomotive Author", x => x.LocoAuthor),
                new TextColumn<Consist, bool>("Is Player Driver", x => x.PlayerDriver),
                new TextColumn<Consist, string>("Locomotive Name", x => x.LocomotiveName),
                new TextColumn<Consist, LocoClass?>("Locomotive Class", x => x.LocoClass),
                new TextColumn<Consist, string>("Service Name", x => x.ServiceName),
            },
        };

        Source.RowSelection!.SingleSelect = false;
    }

    private async Task<string> GetSavedConsistRailVehicleElement()
    {
        ArgumentNullException.ThrowIfNull(SelectedConsist);

        var doc = await Scenario.GetXmlDocument();

        var vehicles = doc
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("ServiceName Key", SelectedConsist.ServiceId)?
            .QuerySelector("RailVehicles");

        if (vehicles is null)
        {
            throw new Exception("could not find consist in scenario bin");
        }

        return vehicles.ToXml();
    }
}
