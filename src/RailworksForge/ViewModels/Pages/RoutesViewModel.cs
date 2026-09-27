using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Echoes;

using RailworksForge.Core;
using RailworksForge.Services;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class RoutesViewModel : ViewModelBase
{
    private const int MaxParallelImageLoads = 4;

    private readonly RouteService _routeService;
    private readonly NavigationService _navigation;
    private readonly ImageService _images;
    private readonly LauncherService _launcher;
    private readonly ClipboardService _clipboard;

    private bool _hasLoadedRoutes;

    public RoutesViewModel(
        RouteService routeService,
        NavigationService navigation,
        ImageService images,
        LauncherService launcher,
        ClipboardService clipboard)
    {
        _routeService = routeService;
        _navigation = navigation;
        _images = images;
        _launcher = launcher;
        _clipboard = clipboard;

        TranslationProvider.OnCultureChanged += (_, _) => OnPropertyChanged(nameof(SearchPlaceholder));
    }

    public SearchableCollection<RouteViewModel> Routes { get; } = new(route => route.SearchIndex);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridLayout))]
    [NotifyPropertyChangedFor(nameof(IsListLayout))]
    public partial RoutesLayout Layout { get; set; } = RoutesLayout.Grid;

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchPlaceholder))]
    public partial int RouteCount { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenRouteCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyRouteNameCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
    public partial RouteViewModel? SelectedRoute { get; set; }

    public bool IsGridLayout => Layout is RoutesLayout.Grid;

    public bool IsListLayout => Layout is RoutesLayout.List;

    public string SearchPlaceholder => string.Format(Utils.GetTranslation("search_routes"), RouteCount);

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
            var routes = await _routeService.GetRoutes().WaitAsync(token);
            var models = routes.Select(route => new RouteViewModel(route)).ToList();
            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = MaxParallelImageLoads,
            };

            Parallel.ForEach(models, options, route => route.ImageBitmap = _images.GetRouteImage(route.Model));

            return models;
        }, models =>
        {
            Routes.Reset(models);
            RouteCount = models.Count;
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
        _navigation.ShowRoute(SelectedRoute!);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRoute))]
    private Task CopyRouteName()
    {
        return _clipboard.SetText(SelectedRoute!.Name);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRoute))]
    private void OpenInExplorer()
    {
        _launcher.OpenDirectory(SelectedRoute!.DirectoryPath);
    }
}
