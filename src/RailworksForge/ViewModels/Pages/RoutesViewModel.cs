using System;
using System.Collections.Generic;
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
    private readonly IRecentRoutesService _recentRoutes;

    private readonly BackgroundImageLoader _imageLoader = new();

    private bool _hasLoadedRoutes;

    public RoutesViewModel(
        IRouteService routeService,
        INavigationService navigation,
        IImageService images,
        ILauncherService launcher,
        IClipboardService clipboard,
        IDispatcher dispatcher,
        IRecentRoutesService recentRoutes)
    {
        _routeService = routeService;
        _navigation = navigation;
        _images = images;
        _launcher = launcher;
        _clipboard = clipboard;
        _dispatcher = dispatcher;
        _recentRoutes = recentRoutes;

        TranslationProvider.OnCultureChanged += (_, _) => OnCultureChanged();
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

    public RangeObservableCollection<RouteViewModel> RecentRoutes { get; } = [];

    public bool HasRecentRoutes => RecentRoutes.Count > 0;

    [ObservableProperty]
    public partial RouteSortOrder SortOrder { get; set; }

    public int SortIndex
    {
        get => (int)SortOrder;
        set
        {
            var sortOrder = (RouteSortOrder)value;

            if (Enum.IsDefined(sortOrder))
            {
                SortOrder = sortOrder;
            }
        }
    }

    public override string StatusText => string.Format(Strings.status_routes.CurrentValue, Routes.Count);

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
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnSortOrderChanged(RouteSortOrder value)
    {
        OnPropertyChanged(nameof(SortIndex));
        ShowRoutes(Routes.Source.ToList());
    }

    private void OnCultureChanged()
    {
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(StatusText));
    }

    private void ShowRoutes(List<RouteViewModel> routes)
    {
        var recentIds = _recentRoutes.GetRecentRouteIds();
        var sorted = SortRoutes(routes, recentIds);

        Routes.Reset(sorted);
        ShowRecentRoutes(routes, recentIds);
        OnPropertyChanged(nameof(StatusText));
    }

    private void ShowRecentRoutes(List<RouteViewModel> routes, IReadOnlyList<string> recentIds)
    {
        var routesById = routes.GroupBy(route => route.Id).ToDictionary(group => group.Key, group => group.First());
        var recent = recentIds.Distinct().Select(id => routesById.GetValueOrDefault(id)).OfType<RouteViewModel>().ToList();

        RecentRoutes.ReplaceWith(recent);
        OnPropertyChanged(nameof(HasRecentRoutes));
    }

    private List<RouteViewModel> SortRoutes(List<RouteViewModel> routes, IReadOnlyList<string> recentIds)
    {
        var byName = routes.OrderBy(route => route.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        if (SortOrder is RouteSortOrder.Name)
        {
            return byName;
        }

        var recentRank = recentIds
            .Select((id, index) => (id, index))
            .DistinctBy(entry => entry.id)
            .ToDictionary(entry => entry.id, entry => entry.index);

        return byName.OrderBy(route => recentRank.GetValueOrDefault(route.Id, int.MaxValue)).ToList();
    }

    private Task LoadRoutes()
    {
        return Loading.RunAsync(Strings.loading_routes.CurrentValue, async token =>
        {
            var routes = await _routeService.GetRoutes().WaitAsync(token);

            return routes.Select(route => new RouteViewModel(route)).ToList();
        }, models =>
        {
            ShowRoutes(models);
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
        var route = SelectedRoute!;

        _recentRoutes.Record(route.Id);
        _navigation.ShowRoute(route);

        var routes = Routes.Source.ToList();

        if (SortOrder is RouteSortOrder.RecentlyOpened)
        {
            ShowRoutes(routes);
        }
        else
        {
            var recentIds = _recentRoutes.GetRecentRouteIds();

            ShowRecentRoutes(routes, recentIds);
        }
    }

    [RelayCommand]
    private void OpenRecentRoute(RouteViewModel route)
    {
        SelectedRoute = route;
        OpenRoute();
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
