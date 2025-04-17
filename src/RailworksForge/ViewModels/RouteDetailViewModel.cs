using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class RouteDetailViewModel : ViewModelBase
{
    [ObservableProperty]
    private RouteViewModel _route;

    private ObservableCollection<Scenario> Scenarios { get; }

    private List<Scenario>? _cachedScenarios;

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenScenarioInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceTrackCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckAssetsCommand { get; }

    private Scenario? SelectedItem => ScenariosSource.RowSelection?.SelectedItem;

    [ObservableProperty]
    private string? _searchTerm;

    public FlatTreeDataGridSource<Scenario> ScenariosSource { get; }

    public RouteDetailViewModel(RouteViewModel route)
    {
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

            await TrackService.ReplaceTracks(Route.Model, result);
        });

        CheckAssetsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await Utils.GetApplicationViewModel().ShowCheckAssetsDialog.Handle(new CheckAssetsViewModel(Route.Model));
        });

        Scenarios = new (GetScenarios());

        ScenariosSource = new FlatTreeDataGridSource<Scenario>(Scenarios)
        {
            Columns =
            {
                new TextColumn<Scenario, PackagingType>("Packaging Type", x => x.PackagingType),
                new TextColumn<Scenario, string>("Name", x => x.Name) { Options = { CanUserSortColumn = true,  }},
                new TextColumn<Scenario, string>("Locomotive", x => x.Locomotive),
                new TextColumn<Scenario, ScenarioClass>("Type", x => x.ScenarioClass),
                new TextColumn<Scenario, int>("Duration", x => x.Duration),
                new TextColumn<Scenario, int>("Rating", x => x.Rating),
                new TextColumn<Scenario, string>("Season", x => x.Season),
                new TextColumn<Scenario, int>("Score", x => x.PlayerInfo.Score),
                new TemplateColumn<Scenario>("Completion", "CompletionCell"),
            },
        };

        this.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not nameof(SearchTerm)) return;

            var invariant = _searchTerm?.ToLowerInvariant();
            var scenarios = _cachedScenarios ?? GetScenarios();
            var indexed = invariant is null ? scenarios : scenarios.Where(scenario => scenario.SearchIndex.Contains(invariant));

            Scenarios.Clear();
            Scenarios.AddRange(indexed);
        };
    }

    protected RouteDetailViewModel(Route route) : this(new RouteViewModel(route)) { }

    private List<Scenario> GetScenarios()
    {
        if (Design.IsDesignMode)
        {
            return [..DesignData.DesignData.Scenarios];
        }

        var items = ScenarioService.GetScenarios(Route.Model);

        _cachedScenarios = items;

        return items;
    }
}
