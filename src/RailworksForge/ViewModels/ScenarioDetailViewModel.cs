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
using RailworksForge.Core.Commands.Common;
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

    [ObservableProperty]
    private string? _searchTerm;

    private List<ConsistViewModel> _cachedServices = [];

    public ObservableCollection<ConsistViewModel> Services { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenBackupsFolder { get; }
    public ReactiveCommand<Unit, Unit> ExportBinXmlCommand { get; }
    public ReactiveCommand<Unit, string> ExportXmlBinCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractScenariosCommand { get; }
    public ReactiveCommand<Unit, Unit> ClickedConsistCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteConsistCommand { get; }

    public IEnumerable<ConsistViewModel> SelectedConsistViewModels { get; set; }

    private ConsistViewModel? SelectedConsistViewModel => SelectedConsistViewModels.Count() is 1 ? SelectedConsistViewModels.First() : null;

    public ScenarioDetailViewModel(Scenario scenario)
    {
        Scenario = scenario;
        SelectedConsistViewModels = [];
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
            if (SelectedConsistViewModel is null) return;

            Utils.GetApplicationViewModel().SelectScenarioConsist(Scenario, SelectedConsistViewModel.Consist);
        });

        SaveConsistCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedConsistViewModel is null)
            {
                return;
            }

            var consistElement = await GetSavedConsistRailVehicleElement();

            var result = await Utils.GetApplicationViewModel().ShowSaveConsistDialog.Handle(new SaveConsistViewModel
            {
                ConsistElement = consistElement,
                Name = SelectedConsistViewModel.Consist.LocomotiveName,
                LocomotiveName = SelectedConsistViewModel.Consist.LocomotiveName,
            });

            if (result?.Name is null) return;

            PersistenceService.SaveConsist(result with
            {
                ConsistElement = consistElement,
            });
        });

        ReplaceConsistCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedConsistViewModels.Any() is false)
            {
                return;
            }

            var result = await Utils.GetApplicationViewModel().ShowReplaceConsistDialog.Handle(new ReplaceConsistViewModel
            {
                AvailableStock = [],
                Scenario = Scenario,
            });

            if (result is null) return;

            IsLoading = true;

            var target = new TargetConsist(SelectedConsistViewModels.Select(x => x.Consist));

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

        DeleteConsistCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedConsistViewModels.Any() is false)
            {
                return;
            }

            var isBulkSelection = SelectedConsistViewModels.Count() > 1;
            var consistMessage = isBulkSelection ? "consists" : "consist";
            var summary = isBulkSelection ? $"{SelectedConsistViewModels.Count()} consists selected." : $"Consist: {SelectedConsistViewModel!.Consist.ServiceName} - {SelectedConsistViewModel.Consist.LocomotiveName}";

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

            IsLoading = true;

            var target = new TargetConsist(SelectedConsistViewModels.Select(x => x.Consist));

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

        this.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not nameof(SearchTerm)) return;

            var invariant = _searchTerm?.ToLowerInvariant();
            var indexed = invariant is null ? _cachedServices : _cachedServices.Where(service => service.Consist.SearchIndex.Contains(invariant));

            Services.Clear();
            Services.AddRange(indexed);
        };
    }

    private async Task<string> GetSavedConsistRailVehicleElement()
    {
        ArgumentNullException.ThrowIfNull(SelectedConsistViewModel);

        using var doc = await Scenario.GetXmlDocument();

        var vehicles = doc
            .QuerySelectorAll("cConsist")
            .QueryByTextContent("ServiceName Key", SelectedConsistViewModel.Consist.ServiceId)?
            .QuerySelector("RailVehicles");

        if (vehicles is null)
        {
            throw new Exception("could not find consist in scenario bin");
        }

        return vehicles.ToXml();
    }

    public void Refresh()
    {
        IsLoading = true;

        var updatedScenario = Scenario.Refresh();

        if (updatedScenario is null) return;

        Observable.Start(GetAllScenarioConsists, RxApp.MainThreadScheduler);
        Dispatcher.UIThread.Post(() => Scenario = updatedScenario);
    }

    private async Task GetAllScenarioConsists()
    {
        using var document = await Scenario.GetXmlDocument(false);
        var consists = document.QuerySelectorAll("cConsist");

        var results = consists
            .Select(Consist.ParseScenarioConsist)
            .Where(r => r is not null)
            .Cast<Consist>()
            .ToList()
            .ConvertAll(e => new ConsistViewModel(e));

        Dispatcher.UIThread.Post(() =>
        {
            Services.Clear();
            Services.AddRange(results);

            _cachedServices = results;

            IsLoading = false;

            Task.Run(() => Parallel.ForEachAsync(results, async (r, _) => await r.LoadImage()));
        });
    }
}
