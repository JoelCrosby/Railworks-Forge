
using System.Reactive;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class NavigationBarViewModel : ViewModelBase
{
    [ObservableProperty]
    private RouteDetailViewModel? _route;

    [ObservableProperty]
    private Scenario? _scenario;

    [ObservableProperty]
    private Consist? _consist;

    public ReactiveCommand<Unit, Unit> RoutesClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectAllRoutes();
    });

    public ReactiveCommand<Unit, Unit> RouteClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectCurrentRoute();
    });

    public ReactiveCommand<Unit, Unit> ScenarioClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectCurrentScenario();
    });

    public ReactiveCommand<Unit, Unit> ConsistClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectCurrentConsist();
    });
}
