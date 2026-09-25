using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input.Platform;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class RouteDetailViewModel : ViewModelBase
{
    private readonly ScenarioService _scenarioService;

    [ObservableProperty]
    private RouteViewModel _route;

    public ObservableCollection<Scenario> Scenarios { get; }

    private List<Scenario>? _cachedScenarios;

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenScenarioInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceTrackCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckAssetsCommand { get; }

    [ObservableProperty]
    private Scenario? _selectedItem;

    [ObservableProperty]
    private string? _searchTerm;

    public RouteDetailViewModel(RouteViewModel route, ScenarioService scenarioService)
    {
        _scenarioService = scenarioService;

        Route = route;

        DetailsClickedCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem is null) return;

            Utils.GetApplicationViewModel().SelectScenario(SelectedItem);
        });

        CopyClickedCommand = ReactiveCommand.CreateFromTask(() =>
        {
            if (SelectedItem is null) return Task.CompletedTask;

            return Clipboard.Get().SetTextAsync(SelectedItem.Name);
        });

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            Launcher.Open(Route.DirectoryPath);
        });

        OpenScenarioInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem?.DirectoryPath is null) return;

            Launcher.Open(SelectedItem.DirectoryPath);
        });

        ReplaceTrackCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await Utils.GetApplicationViewModel().ShowReplaceTrackDialog.Handle(new ReplaceTrackViewModel(Route.Model));

            if (result is null) return;

            await Loading.RunAsync("Replacing tracks…", _ => TrackService.ReplaceTracks(Route.Model, result));
        });

        CheckAssetsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await Utils.GetApplicationViewModel().ShowCheckAssetsDialog.Handle(new CheckAssetsViewModel(Route.Model));
        });

        Scenarios = [];

        if (!Design.IsDesignMode)
        {
            _ = Loading.RunAsync("Loading scenarios…", token => Task.FromResult(GetScenarios(token)), items =>
            {
                _cachedScenarios = items;
                FilterScenarios();
            });
        }
        else
        {
            Scenarios.AddRange(GetScenarios());
        }

        this.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not nameof(SearchTerm)) return;

            FilterScenarios();
        };
    }

    private void FilterScenarios()
    {
        var invariant = SearchTerm?.ToLowerInvariant();
        var scenarios = _cachedScenarios ?? [];
        var indexed = invariant is null ? scenarios : scenarios.Where(scenario => scenario.SearchIndex.Contains(invariant));
        Scenarios.Clear();
        Scenarios.AddRange(indexed);
    }

    private List<Scenario> GetScenarios(CancellationToken cancellationToken = default)
    {
        if (Design.IsDesignMode)
        {
            return [..DesignData.DesignData.Scenarios];
        }

        var items = _scenarioService.GetScenarios(Route.Model, cancellationToken);

        return items;
    }
}
