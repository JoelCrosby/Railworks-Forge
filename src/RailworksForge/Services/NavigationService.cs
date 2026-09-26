using System;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Services;

public partial class NavigationService(IServiceProvider services) : ObservableObject
{
    private RoutesViewModel? _routesPage;
    private ScenarioDetailViewModel? _scenarioPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    public partial ViewModelBase? CurrentPage { get; set; }

    [ObservableProperty]
    public partial RouteViewModel? CurrentRoute { get; set; }

    [ObservableProperty]
    public partial Scenario? CurrentScenario { get; set; }

    [ObservableProperty]
    public partial Consist? CurrentConsist { get; set; }

    public bool IsSettingsActive => CurrentPage is SettingsViewModel;

    public void ShowRoutes()
    {
        CurrentRoute = null;
        CurrentScenario = null;
        CurrentConsist = null;

        _routesPage ??= services.GetRequiredService<RoutesViewModel>();

        Show(_routesPage);
    }

    public void ShowRoute(RouteViewModel route)
    {
        CurrentRoute = route;
        CurrentScenario = null;
        CurrentConsist = null;

        Show(Create<RouteDetailViewModel>(route));
    }

    public void ShowScenario(Scenario scenario)
    {
        CurrentScenario = scenario;
        CurrentConsist = null;

        _scenarioPage = Create<ScenarioDetailViewModel>(scenario);

        Show(_scenarioPage);
    }

    public void ShowConsist(Scenario scenario, Consist consist)
    {
        CurrentConsist = consist;

        Show(Create<ConsistDetailViewModel>(scenario, consist));
    }

    public void ShowSettings()
    {
        if (CurrentPage is SettingsViewModel)
        {
            return;
        }

        Show(services.GetRequiredService<SettingsViewModel>());
    }

    public void ShowCurrentRoute()
    {
        if (CurrentRoute is null)
        {
            return;
        }

        ShowRoute(CurrentRoute);
    }

    public void ShowCurrentScenario()
    {
        CurrentConsist = null;

        if (_scenarioPage is null)
        {
            return;
        }

        Show(_scenarioPage);
    }

    public void ShowCurrentConsist()
    {
        if (_scenarioPage is null || CurrentConsist is null)
        {
            return;
        }

        ShowConsist(_scenarioPage.Scenario, CurrentConsist);
    }

    private TPage Create<TPage>(params object[] arguments) where TPage : ViewModelBase
    {
        return ActivatorUtilities.CreateInstance<TPage>(services, arguments);
    }

    // Showing the current page again reloads it, which is how the breadcrumbs refresh a page.
    private void Show(ViewModelBase page)
    {
        var isNewPage = !ReferenceEquals(CurrentPage, page);

        if (isNewPage)
        {
            CurrentPage?.Deactivate();
            CurrentPage = page;
        }

        _ = page.Activate();
    }
}
