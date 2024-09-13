using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AngleSharp.Xml;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.Commands;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.models;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ScenarioDetailViewModel : ViewModelBase
{
    [ObservableProperty]
    private Scenario _scenario;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<Consist> Services { get; }

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
        IsLoading = true;

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

            var request = new ReplaceConsistRequest
            {
                Target = target,
                PreloadConsist = result,
            };

            var runner = new ConsistCommandRunner
            {
                Scenario = _scenario,
                Commands = [new ReplaceConsist(request)],
            };

            await runner.Run();

            Refresh();
        });

        LoadAcquisitionStates = ReactiveCommand.CreateFromObservable(() =>
        {
            return Observable.StartAsync(async () =>
            {
                await Scenario.GetConsistStatus();

                Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(Services)));
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

            var runner = new ConsistCommandRunner
            {
                Scenario = _scenario,
                Commands = [new DeleteConsist(target)],
            };

            await runner.Run();

            Refresh();
        });

        Services = [];

        Observable.Start(GetAllScenarioConsists, RxApp.MainThreadScheduler);
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

    public void Refresh()
    {
        var updatedScenario = Scenario.Refresh();

        if (updatedScenario is null) return;

        Observable.Start(GetAllScenarioConsists, RxApp.MainThreadScheduler);
        Dispatcher.UIThread.Post(() => Scenario = updatedScenario);
    }

    private async Task GetAllScenarioConsists()
    {
        var document = await Scenario.GetXmlDocument(false);
        var consists = document.QuerySelectorAll("cConsist");
        var results = consists.Select(Consist.ParseScenarioConsist);

        Services.Clear();
        Services.AddRange(results);

        IsLoading = false;
    }
}
