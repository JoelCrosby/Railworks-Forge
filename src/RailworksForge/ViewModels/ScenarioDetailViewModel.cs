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
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

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
    private string? _searchTerm;

    private List<ConsistViewModel> _cachedServices = [];

    public ObservableCollection<ConsistViewModel> Services { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenBackupsFolder { get; }
    public ReactiveCommand<Unit, Unit> ExportBinXmlCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportXmlBinCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractScenariosCommand { get; }
    public ReactiveCommand<Unit, Unit> ClickedConsistCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteConsistCommand { get; }

    public IReadOnlyList<ConsistViewModel> SelectedItems { get; set; } = [];

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
            await Loading.RunAsync("Exporting scenario XML…", _ => scenario.ExportBinToXml(), path =>
            {

                if (Path.GetDirectoryName(path) is {} directory)
                {
                    Launcher.Open(directory);
                }
            }, allowRetry: false);
        });

        ExportXmlBinCommand = ReactiveCommand.CreateFromTask(() =>
            Loading.RunAsync("Converting scenario XML…", _ => scenario.ConvertXmlToBin()));

        ExtractScenariosCommand = ReactiveCommand.CreateFromTask(() =>
            Loading.RunAsync("Extracting scenarios…", _ =>
            {
                scenario.Route.ExtractScenarios();

                return Task.CompletedTask;
            }));

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

            string? consistElement = null;
            await Loading.RunAsync("Preparing consist…", _ => GetSavedConsistRailVehicleElement(),
                xml => consistElement = xml, allowRetry: false);

            if (consistElement is null)
            {
                return;
            }

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

            await Loading.RunAsync("Updating scenario…", _ => runner.Run());

            if (!Loading.HasError && IsActive)
            {
                await LoadScenario();
            }
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

            var target = new TargetConsist(SelectedItems.Select(x => x.Consist));

            var runner = new ConsistCommandRunner
            {
                Scenario = _scenario,
                Commands = [new DeleteConsist(target)],
            };

            await Loading.RunAsync("Updating scenario…", _ => runner.Run());

            if (!Loading.HasError && IsActive)
            {
                await LoadScenario();
            }
        });

        Services = [];

        Refresh();

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
        _ = LoadScenario();
    }

    private Task LoadScenario()
    {
        var scenario = Scenario;

        return Loading.RunAsync("Loading scenario services…", async token =>
        {
            Cache.BlueprintAcquisitionStates.Clear();
            Cache.ArchiveCache.Clear();
            var updated = scenario.Refresh() ?? throw new InvalidOperationException("The scenario could not be loaded.");
            using var document = await updated.GetXmlDocument(false);
            token.ThrowIfCancellationRequested();
            Cache.ConsistAcquisitionStates.Clear();
            var results = document.QuerySelectorAll("cConsist")
                .Select(Consist.ParseScenarioConsist)
                .OfType<Consist>()
                .Select(consist => new ConsistViewModel(consist))
                .ToList();

            foreach (var result in results)
            {
                token.ThrowIfCancellationRequested();
                result.LoadImage();
            }

            return (Scenario: updated, Services: results);
        }, result =>
        {
            Scenario = result.Scenario;
            _cachedServices = result.Services;
            var invariant = SearchTerm?.ToLowerInvariant();
            var services = invariant is null
                ? _cachedServices
                : _cachedServices.Where(service => service.Consist.SearchIndex.Contains(invariant));
            Services.Clear();
            Services.AddRange(services);
        });
    }
}
