using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class RouteDetailViewModel(
    RouteViewModel route,
    ScenarioService scenarioService,
    TrackService trackService,
    ImageService images,
    NavigationService navigation,
    DialogService dialogs,
    LauncherService launcher,
    ClipboardService clipboard) : ViewModelBase
{
    private CancellationTokenSource? _imageLoading;

    public RouteViewModel Route { get; } = route;

    public SearchableCollection<ScenarioRowViewModel> Scenarios { get; } = new(row => row.Scenario.SearchIndex);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyScenarioNameCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenScenarioInExplorerCommand))]
    public partial ScenarioRowViewModel? SelectedScenario { get; set; }

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    private bool HasSelectedScenario => SelectedScenario is not null;

    protected override Task OnActivated()
    {
        scenarioService.PlayerInfoUpdated += OnPlayerInfoUpdated;

        return Loading.RunAsync(
            "Loading scenarios…",
            token => Task.FromResult(scenarioService.GetScenarios(Route.Model, token)),
            ShowScenarios);
    }

    protected override void OnDeactivated()
    {
        _imageLoading?.Cancel();
        scenarioService.PlayerInfoUpdated -= OnPlayerInfoUpdated;
    }

    partial void OnSearchTermChanged(string? value)
    {
        Scenarios.Filter(value);
    }

    private void ShowScenarios(List<Scenario> scenarios)
    {
        scenarioService.RefreshPlayerInfo(scenarios);

        var rows = scenarios.ConvertAll(scenario => new ScenarioRowViewModel(scenario));

        Scenarios.Reset(rows);
        LoadLocomotiveImages(rows);
    }

    private void OnPlayerInfoUpdated()
    {
        Dispatcher.UIThread.Post(RefreshPlayerInfo);
    }

    private void RefreshPlayerInfo()
    {
        var scenarios = Scenarios.Source.Select(row => row.Scenario);

        scenarioService.RefreshPlayerInfo(scenarios);

        foreach (var row in Scenarios.Source)
        {
            row.RefreshPlayerInfo();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedScenario))]
    private void OpenScenario()
    {
        navigation.ShowScenario(SelectedScenario!.Scenario);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedScenario))]
    private Task CopyScenarioName()
    {
        return clipboard.SetText(SelectedScenario!.Scenario.Name);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedScenario))]
    private void OpenScenarioInExplorer()
    {
        launcher.OpenDirectory(SelectedScenario!.Scenario.DirectoryPath);
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        launcher.OpenDirectory(Route.DirectoryPath);
    }

    [RelayCommand]
    private async Task ReplaceTrack()
    {
        var dialog = dialogs.Create<ReplaceTrackViewModel>(Route.Model);
        var request = await dialogs.Show(dialog);

        if (request is null)
        {
            return;
        }

        await Loading.RunAsync("Replacing tracks…", _ => trackService.ReplaceTracks(Route.Model, request));
    }

    [RelayCommand]
    private Task CheckAssets()
    {
        var dialog = dialogs.Create<CheckAssetsViewModel>(Route.Model);

        return dialogs.Show(dialog);
    }

    // Images come from disk or inside .ap archives, so they load after the list is shown and fill in as they are found.
    private void LoadLocomotiveImages(List<ScenarioRowViewModel> rows)
    {
        _imageLoading?.Cancel();
        var cancellation = new CancellationTokenSource();
        _imageLoading = cancellation;
        var token = cancellation.Token;

        _ = Task.Run(() =>
        {
            foreach (var row in rows)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (row.Scenario.PlayerConsist is not { BlueprintId.Length: > 0 } playerConsist)
                {
                    continue;
                }

                var image = images.GetBlueprintImage(playerConsist);

                if (image is not null)
                {
                    Dispatcher.UIThread.Post(() => row.LocomotiveImage = image);
                }
            }
        }, token);
    }
}
