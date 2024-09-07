using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AngleSharp.Xml;

using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

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

    public FlatTreeDataGridSource<Consist> ServicesSource { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenBackupsFolder { get; }
    public ReactiveCommand<Unit, Unit> ExportBinXmlCommand { get; }
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

        OpenBackupsFolder = ReactiveCommand.Create(() =>
        {
            Launcher.Open(Scenario.BackupDirectory);
        });

        ExportBinXmlCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = await scenario.ExportBinToXml();

            if (Path.GetDirectoryName(path) is {} dirname)
            {
                Launcher.Open(dirname);
            }
        });

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
            return Observable.StartAsync(async () =>
            {
                await Scenario.GetConsistStatus();

                Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(ServicesSource)));
            }, RxApp.TaskpoolScheduler);
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

        ServicesSource = new FlatTreeDataGridSource<Consist>(Scenario.Consists)
        {
            Columns =
            {
                new TextColumn<Consist, AcquisitionState>("Locomotive State", c => c.AcquisitionState),
                new TextColumn<Consist, AcquisitionState>("Consist State", c => c.ConsistAcquisitionState),
                new TextColumn<Consist, string>("Locomotive Author", c => c.LocoAuthor),
                new TextColumn<Consist, bool>("Is Player Driver", c => c.PlayerDriver),
                new TextColumn<Consist, string>("Locomotive Name", c => c.LocomotiveName),
                new TextColumn<Consist, LocoClass?>("Locomotive Class", c => c.LocoClass),
                new TextColumn<Consist, string>("Service Name", c => c.ServiceName),
            },
        };
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
        var updatedScenario = Scenario.Refresh();

        Dispatcher.UIThread.Post(() =>
        {
            Scenario = updatedScenario;
            ServicesSource.Items = updatedScenario.Consists;

            OnPropertyChanged(nameof(ServicesSource));
        });
    }
}
