using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class RouteDetailViewModel : ViewModelBase
{
    [ObservableProperty]
    private RouteViewModel _route;

    public ObservableCollection<Scenario> Scenarios { get; init; }

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenScenarioInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceTrackCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckAssetsCommand { get; }

    public Scenario? SelectedItem { get; set; }

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

        Scenarios = GetScenarios();
    }

    protected RouteDetailViewModel(Route route) : this(new RouteViewModel(route)) { }

    private ObservableCollection<Scenario> GetScenarios()
    {
        if (Design.IsDesignMode)
        {
            return new (DesignData.DesignData.Scenarios);
        }

        var items = ScenarioService.GetScenarios(Route.Model);

        return new (items);
    }
}
