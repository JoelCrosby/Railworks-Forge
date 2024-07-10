using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class RoutesBaseViewModel : ViewModelBase
{
    public ObservableCollection<RouteViewModel> ListItems { get; } = [];

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }

    public RouteViewModel? SelectedItem { get; set; }

    private List<RouteViewModel>? _cachedRoutes;

    public RoutesBaseViewModel()
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
    }

    public async Task GetAllRoutes(string? searchTerm = null)
    {
        var invariant = searchTerm?.ToLowerInvariant();
        var routes = _cachedRoutes ?? await LoadRoutes();
        var results = invariant is null ? routes : routes.Where(route => route.SearchIndex.Contains(invariant));

        ListItems.Clear();
        ListItems.AddRange(results);
    }

    private async ValueTask<List<RouteViewModel>> LoadRoutes()
    {
        var items = RouteService.GetRoutes();
        var models = items.Select(item => new RouteViewModel(item)).ToList();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8,
        };

        await Parallel.ForEachAsync(models, options, (route, _) => route.LoadImage());

        _cachedRoutes = models;

        return _cachedRoutes;
    }
}
