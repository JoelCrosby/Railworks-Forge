using System;
using System.Threading.Tasks;

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
    private Task ShowRoutes()
    {
        return LeaveCurrentPage(Navigation.ShowRoutes);
    }

    [RelayCommand]
    private Task ShowRoute()
    {
        return LeaveCurrentPage(Navigation.ShowCurrentRoute);
    }

    [RelayCommand]
    private Task ShowScenario()
    {
        return LeaveCurrentPage(Navigation.ShowCurrentScenario);
    }

    [RelayCommand]
    private Task ShowConsist()
    {
        return LeaveCurrentPage(Navigation.ShowCurrentConsist);
    }

    [RelayCommand]
    private Task ShowSettings()
    {
        return LeaveCurrentPage(Navigation.ShowSettings);
    }

    private async Task LeaveCurrentPage(Action navigate)
    {
        var canLeave = Navigation.CurrentPage is not {} page || await page.CanLeave();

        if (canLeave)
        {
            navigate();
        }
    }
}
