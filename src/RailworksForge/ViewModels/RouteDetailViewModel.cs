using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Threading;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class RouteDetailViewModel : ViewModelBase
{
    public RouteViewModel Route { get; }

    public ObservableCollection<Scenario> Scenarios { get; }

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }

    public Scenario? SelectedItem { get; set; }

    public RouteDetailViewModel(RouteViewModel route)
    {
        Route = route;
        Scenarios = new ObservableCollection<Scenario>();

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
            if (SelectedItem is null) return;

            Launcher.Open(SelectedItem.Path);
        });

        GetScenarios();
    }

    private void GetScenarios()
    {
        var items = ScenarioService.GetScenarios(Route.Model);

        Dispatcher.UIThread.Post(() =>
        {
            Scenarios.AddRange(items);
        });
    }
}
