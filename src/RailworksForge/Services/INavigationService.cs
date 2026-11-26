using System.ComponentModel;

using RailworksForge.Core.Consists;
using RailworksForge.Core.Scenarios;
using RailworksForge.ViewModels;

namespace RailworksForge.Services;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentPage { get; }

    RouteViewModel? CurrentRoute { get; }

    Scenario? CurrentScenario { get; }

    Consist? CurrentConsist { get; }

    bool IsSettingsActive { get; }

    IScenarioEditor? CurrentEditor { get; }

    void ShowRoutes();

    void ShowRoute(RouteViewModel route);

    void ShowScenario(Scenario scenario);

    void ShowConsist(IScenarioEditor editor, Consist consist);

    void ShowSettings();

    void ShowCurrentRoute();

    void ShowCurrentScenario();

    void ShowCurrentConsist();
}
