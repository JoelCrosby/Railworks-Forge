using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Services;

namespace RailworksForge.ViewModels;

public partial class NavigationBarViewModel(NavigationService navigation) : ObservableObject
{
    public NavigationService Navigation { get; } = navigation;

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
