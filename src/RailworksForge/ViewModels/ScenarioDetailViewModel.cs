using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AngleSharp.Xml;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ScenarioDetailViewModel : ViewModelBase
{
    [ObservableProperty]
    private Scenario _scenario;

    public ObservableCollection<Consist> Services { get; } = [];

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, string> ExportBinXmlCommand { get; }
    public ReactiveCommand<Unit, string> ExportXmlBinCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractScenariosCommand { get; }
    public ReactiveCommand<Unit, Unit> ClickedConsistCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadAcquisitionStates { get; }

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

        ExportBinXmlCommand = ReactiveCommand.CreateFromTask(() => scenario.ConvertBinToXml(false));
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

            Refresh();
        });

        LoadAcquisitionStates = ReactiveCommand.CreateFromObservable(() =>
        {
            return Observable.StartAsync(() => Scenario.GetConsistStatus(), RxApp.TaskpoolScheduler);
        });

        DeleteConsistCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedConsists.Any() is false)
            {
                return;
            }

            var isBulkSelection = SelectedConsists.Count() > 1;
            var consistMessage = isBulkSelection ? "consists" : "consist";
            var summary = isBulkSelection ? $"{SelectedConsists.Count()} consists selected." : $"Consist: {SelectedConsist!.ServiceName} - {SelectedConsist.LocomotiveName}";

            var result = await Utils.GetApplicationViewModel().ShowConfirmationDialog.Handle(new ConfirmationDialogViewModel
            {
                AcceptLabel = $"Delete {consistMessage}",
                Title = "Delete Consist",
                BodyText = $"""
                            Are you sure you wish to delete the selected {consistMessage}?
                            
                            {summary}
                            """,
            });

            if (!result) return;

            var target = new TargetConsist(SelectedConsists);

            await ConsistService.DeleteConsist(target, Scenario);

            Refresh();
        });

        Services.AddRange(Scenario.Consists);
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

    private void Refresh()
    {
        Scenario = Scenario.Refresh();

        Services.Clear();
        Services.AddRange(Scenario.Consists);

        OnPropertyChanging(nameof(Services));
    }
}
