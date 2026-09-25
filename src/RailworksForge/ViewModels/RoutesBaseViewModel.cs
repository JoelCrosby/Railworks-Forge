using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Input.Platform;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class RoutesBaseViewModel : ViewModelBase
{
    public ObservableCollection<RouteViewModel> ListItems { get; } = [];

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }

    public RouteViewModel? SelectedItem { get; set; }

    private readonly Lock _routeLock = new();
    private Task<List<RouteViewModel>>? _routeLoad;

    public RoutesBaseViewModel()
    {
        IsLoading = true;

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
    }

    public Task GetAllRoutes(string? searchTerm = null)
    {
        return Loading.RunAsync("Loading routes…", async token =>
        {
            var routes = await GetRoutesTask().WaitAsync(token);
            token.ThrowIfCancellationRequested();
            var invariant = searchTerm?.ToLowerInvariant();
            var results = invariant is null ? routes : routes.Where(route => route.SearchIndex.Contains(invariant));

            return results.ToList();
        }, result =>
        {
            ListItems.Clear();
            ListItems.AddRange(result);
        });
    }

    private Task<List<RouteViewModel>> GetRoutesTask()
    {
        lock (_routeLock)
        {

            if (_routeLoad is null || _routeLoad.IsFaulted || _routeLoad.IsCanceled)
            {
                _routeLoad = LoadRoutes();
            }

            return _routeLoad;
        }
    }

    private static async Task<List<RouteViewModel>> LoadRoutes()
    {
        var items = RouteService.GetRoutes();
        var models = items.Select(item => new RouteViewModel(item)).ToList();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        await Parallel.ForEachAsync(models, options, (route, _) => route.LoadImage());

        return models;
    }
}
