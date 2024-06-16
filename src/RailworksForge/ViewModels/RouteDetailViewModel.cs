using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Examples;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class RouteDetailViewModel : ViewModelBase
{
    public RouteViewModel Route { get; }

    public IObservable<ObservableCollection<Scenario>> Scenarios { get; init; }

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenScenarioInExplorerCommand { get; }

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

        Scenarios = Observable.Start(GetScenarios, RxApp.TaskpoolScheduler);
    }

    protected RouteDetailViewModel(Route route) : this(new RouteViewModel(route)) { }

    private ObservableCollection<Scenario> GetScenarios()
    {
        if (Design.IsDesignMode)
        {
            return new (Example.Scenarios);
        }

        var items = ScenarioService.GetScenarios(Route.Model);

        return new (items);
    }
}
