using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Services;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class NavigationBarViewModel : ObservableObject
{
    private readonly DialogService _dialogs;

    public NavigationBarViewModel(NavigationService navigation, ToolsMenuViewModel tools, DialogService dialogs)
    {
        Navigation = navigation;
        Tools = tools;
        _dialogs = dialogs;

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
        return DiscardEditsThen(Navigation.ShowRoutes);
    }

    [RelayCommand]
    private Task ShowRoute()
    {
        return DiscardEditsThen(Navigation.ShowCurrentRoute);
    }

    // Showing the current page again reloads it from disk, so only that discards pending edits.
    [RelayCommand]
    private Task ShowScenario()
    {
        return Navigation.CurrentPage is ScenarioDetailViewModel
            ? DiscardEditsThen(Navigation.ShowCurrentScenario)
            : Show(Navigation.ShowCurrentScenario);
    }

    [RelayCommand]
    private Task ShowConsist()
    {
        return Navigation.CurrentPage is ConsistDetailViewModel
            ? DiscardEditsThen(Navigation.ShowCurrentConsist)
            : Show(Navigation.ShowCurrentConsist);
    }

    [RelayCommand]
    private void ShowSettings()
    {
        Navigation.ShowSettings();
    }

    private static Task Show(Action navigate)
    {
        navigate();

        return Task.CompletedTask;
    }

    private async Task DiscardEditsThen(Action navigate)
    {
        if (Navigation.CurrentEditor is { HasPendingChanges: true } editor)
        {
            var confirmation = new ConfirmationDialogViewModel
            {
                Title = Strings.scenario_unsaved_changes.CurrentValue,
                BodyText = Strings.scenario_unsaved_changes_body.CurrentValue,
                AcceptLabel = Strings.discard_changes.CurrentValue,
            };

            if (!await _dialogs.Show(confirmation))
            {
                return;
            }

            editor.Discard();
        }

        navigate();
    }
}
