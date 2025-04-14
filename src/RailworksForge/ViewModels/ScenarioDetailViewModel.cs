using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    private ObservableCollection<ConsistViewModel> Services { get; }
    public FlatTreeDataGridSource<ConsistViewModel> ServicesSource { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenBackupsFolder { get; }
    public ReactiveCommand<Unit, Unit> ExportBinXmlCommand { get; }
    public ReactiveCommand<Unit, string> ExportXmlBinCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractScenariosCommand { get; }
    public ReactiveCommand<Unit, Unit> ClickedConsistCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteConsistCommand { get; }

    private IReadOnlyList<ConsistViewModel> SelectedItems => ServicesSource.RowSelection?.SelectedItems as IReadOnlyList<ConsistViewModel> ?? [];

    private ConsistViewModel? SelectedConsistViewModel => SelectedItems.Count is 1 ? SelectedItems[0] : null;

    public ScenarioDetailViewModel(Scenario scenario)
    {
        Scenario = scenario;
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
            if (SelectedItems.Any() is false)
            {
                return;
            }

            var result = await Utils.GetApplicationViewModel().ShowReplaceConsistDialog.Handle(new ReplaceConsistViewModel
            {
                Scenario = Scenario,
            });

            if (result is null) return;

            IsLoading = true;

            var target = new TargetConsist(SelectedItems.Select(x => x.Consist));

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
            if (SelectedItems.Any() is false)
            {
                return;
            }

            var isBulkSelection = SelectedItems.Count > 1;
            var consistMessage = isBulkSelection ? "consists" : "consist";
            var summary = isBulkSelection ? $"{SelectedItems.Count} consists selected." : $"Consist: {SelectedConsistViewModel!.Consist.ServiceName} - {SelectedConsistViewModel.Consist.LocomotiveName}";

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

            var target = new TargetConsist(SelectedItems.Select(x => x.Consist));

            var runner = new ConsistCommandRunner
            {
                Scenario = _scenario,
                Commands = [new DeleteConsist(target)],
            };

            await runner.Run();

            Refresh();
        });

        Services = [];

        ServicesSource = new FlatTreeDataGridSource<ConsistViewModel>(Services)
        {
            Columns =
            {
                new TemplateColumn<ConsistViewModel>("Image", "ImageCell"),
                new TextColumn<ConsistViewModel, AcquisitionState>("Consist State", x => x.Consist.ConsistAcquisitionState),
                new TextColumn<ConsistViewModel, bool>("Is Player Driver", x => x.Consist.PlayerDriver),
                new TextColumn<ConsistViewModel, string>("Locomotive Name", x => x.Consist.LocomotiveName),
                new TextColumn<ConsistViewModel, int>("Consist Length", x => x.Consist.Length),
                new TextColumn<ConsistViewModel, string>("Service Name", x => x.Consist.ServiceName),
                new TextColumn<ConsistViewModel, string>("Provider", x => x.Consist.BlueprintSetIdProvider),
                new TextColumn<ConsistViewModel, string>("Product", x => x.Consist.BlueprintSetIdProduct),
                new TextColumn<ConsistViewModel, string>("Blueprint ID", x => x.Consist.BlueprintId),
            },
        };

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

        Cache.ConsistAcquisitionStates.Clear();

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

            Task.Run(() =>
            {
                foreach (var result in results)
                {
                    result.LoadImage();
                }
            });
        });
    }
}
