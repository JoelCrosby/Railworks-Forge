using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Threading;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class RoutesListViewModel : ViewModelBase
{
    public IObservable<ObservableCollection<RouteViewModel>> ListItems { get; }

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }

    public RouteViewModel? SelectedItem { get; set; }

    public RoutesListViewModel()
    {
        CopyClickedCommand = ReactiveCommand.CreateFromTask(() =>
        {
            if (SelectedItem is null) return Task.CompletedTask;

            return Clipboard.Get().SetTextAsync(SelectedItem.Name);
        });

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem is null) return;

            Launcher.Open(SelectedItem.DirectoryPath);
        });

        DetailsClickedCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem is null) return;

            Utils.GetApplicationViewModel().SelectRoute(SelectedItem);
        });

        ListItems = Observable.Start(LoadRoutes, RxApp.TaskpoolScheduler);
    }

    public ObservableCollection<RouteViewModel> LoadRoutes()
    {
        var items = RouteService.GetRoutes();
        var models = items.Select(item => new RouteViewModel(item)).ToList();

        LoadImages(models);

        return new ObservableCollection<RouteViewModel>(models);
    }

    private async void LoadImages(IEnumerable<RouteViewModel> items)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8,
        };

        await Parallel.ForEachAsync(items, options, async (route, _) => await route.LoadImage());
    }
}
