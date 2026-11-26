using System.Linq;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Echoes;

using RailworksForge.Core.Routes;
using RailworksForge.Services;
using RailworksForge.Translations;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class RoutesViewModel : ViewModelBase
{
    private readonly IRouteService _routeService;
    private readonly INavigationService _navigation;
    private readonly IImageService _images;
    private readonly ILauncherService _launcher;
    private readonly IClipboardService _clipboard;
    private readonly IDispatcher _dispatcher;

    private readonly BackgroundImageLoader _imageLoader = new();

    private bool _hasLoadedRoutes;

    public RoutesViewModel(
        IRouteService routeService,
        INavigationService navigation,
        IImageService images,
        ILauncherService launcher,
        IClipboardService clipboard,
        IDispatcher dispatcher)
    {
        _routeService = routeService;
        _navigation = navigation;
        _images = images;
        _launcher = launcher;
        _clipboard = clipboard;
        _dispatcher = dispatcher;

        TranslationProvider.OnCultureChanged += (_, _) => OnPropertyChanged(nameof(SearchPlaceholder));
        _routeService.RoutesInvalidated += OnRoutesInvalidated;
    }

    // Raised from whichever thread invalidated the routes, e.g. a package install.
    private void OnRoutesInvalidated()
    {
        _dispatcher.Post(ReloadRoutes);
    }

    private void ReloadRoutes()
    {
        _hasLoadedRoutes = false;

        if (IsActive)
        {
            _ = LoadRoutes();
        }
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

    public string SearchPlaceholder => string.Format(Strings.search_routes.CurrentValue, RouteCount);

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
        return Loading.RunAsync(Strings.loading_routes.CurrentValue, async token =>
        {
            var routes = await _routeService.GetRoutes().WaitAsync(token);

            return routes.Select(route => new RouteViewModel(route)).ToList();
        }, models =>
        {
            Routes.Reset(models);
            RouteCount = models.Count;
            _hasLoadedRoutes = true;
            _imageLoader.Load(models, route => _images.GetRouteImage(route.Model), (route, image) => route.ImageBitmap = image);
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
