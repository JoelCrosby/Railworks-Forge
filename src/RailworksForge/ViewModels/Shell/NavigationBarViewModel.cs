using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Services;

namespace RailworksForge.ViewModels;

public partial class NavigationBarViewModel : ObservableObject
{
    public NavigationBarViewModel(NavigationService navigation, ToolsMenuViewModel tools)
    {
        Navigation = navigation;
        Tools = tools;

        navigation.PropertyChanged += (_, _) => OnNavigationChanged();
    }

    public NavigationService Navigation { get; }

    public ToolsMenuViewModel Tools { get; }

    public RoutesViewModel? RoutesPage => Navigation.CurrentPage as RoutesViewModel;

    public RouteDetailViewModel? RouteDetailPage => Navigation.CurrentPage as RouteDetailViewModel;

    public bool IsRoutesCrumbCurrent => Navigation.CurrentRoute is null && !Navigation.IsSettingsActive;

    public bool IsRouteCrumbCurrent => Navigation.CurrentScenario is null && !Navigation.IsSettingsActive;

    public bool IsScenarioCrumbCurrent => Navigation.CurrentConsist is null && !Navigation.IsSettingsActive;

    public bool IsConsistCrumbCurrent => !Navigation.IsSettingsActive;

    private void OnNavigationChanged()
    {
        OnPropertyChanged(nameof(RoutesPage));
        OnPropertyChanged(nameof(RouteDetailPage));
        OnPropertyChanged(nameof(IsRoutesCrumbCurrent));
        OnPropertyChanged(nameof(IsRouteCrumbCurrent));
        OnPropertyChanged(nameof(IsScenarioCrumbCurrent));
        OnPropertyChanged(nameof(IsConsistCrumbCurrent));
    }

    [RelayCommand]
    private void ShowRoutes()
    {
        Navigation.ShowRoutes();
    }

    [RelayCommand]
    private void ShowRoute()
    {
        Navigation.ShowCurrentRoute();
    }

    [RelayCommand]
    private void ShowScenario()
    {
        Navigation.ShowCurrentScenario();
    }

    [RelayCommand]
    private void ShowConsist()
    {
        Navigation.ShowCurrentConsist();
    }

    [RelayCommand]
    private void ShowSettings()
    {
        Navigation.ShowSettings();
    }
}
