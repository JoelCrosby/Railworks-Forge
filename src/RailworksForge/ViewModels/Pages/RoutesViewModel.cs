using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Services;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public enum RoutesLayout
{
    Grid,
    List,
}

public partial class RoutesViewModel(
    RouteService routeService,
    NavigationService navigation,
    ImageService images,
    LauncherService launcher,
    ClipboardService clipboard) : ViewModelBase
{
    private const int MaxParallelImageLoads = 4;

    private bool _hasLoadedRoutes;

    public SearchableCollection<RouteViewModel> Routes { get; } = new(route => route.SearchIndex);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridLayout))]
    [NotifyPropertyChangedFor(nameof(IsListLayout))]
    public partial RoutesLayout Layout { get; set; } = RoutesLayout.Grid;

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenRouteCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyRouteNameCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
    public partial RouteViewModel? SelectedRoute { get; set; }

    public bool IsGridLayout => Layout is RoutesLayout.Grid;

    public bool IsListLayout => Layout is RoutesLayout.List;

    private bool HasSelectedRoute => SelectedRoute is not null;

    protected override Task OnActivated()
    {
        return _hasLoadedRoutes ? Task.CompletedTask : LoadRoutes();
    }

    partial void OnSearchTermChanged(string? value)
    {
        Routes.Filter(value);
    }

    private Task LoadRoutes()
    {
        return Loading.RunAsync("Loading routes…", async token =>
        {
            var routes = await routeService.GetRoutes().WaitAsync(token);
            var models = routes.Select(route => new RouteViewModel(route)).ToList();
            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = MaxParallelImageLoads,
            };

            Parallel.ForEach(models, options, route => route.ImageBitmap = images.GetRouteImage(route.Model));

            return models;
        }, models =>
        {
            Routes.Reset(models);
            _hasLoadedRoutes = true;
        });
    }

    [RelayCommand]
    private void ShowGrid()
    {
        Layout = RoutesLayout.Grid;
    }

    [RelayCommand]
    private void ShowList()
    {
        Layout = RoutesLayout.List;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRoute))]
    private void OpenRoute()
    {
        navigation.ShowRoute(SelectedRoute!);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRoute))]
    private Task CopyRouteName()
    {
        return clipboard.SetText(SelectedRoute!.Name);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRoute))]
    private void OpenInExplorer()
    {
        launcher.OpenDirectory(SelectedRoute!.DirectoryPath);
    }
}
