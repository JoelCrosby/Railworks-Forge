using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;
using RailworksForge.Translations;

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
    private readonly BackgroundImageLoader _imageLoader = new();

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
            Strings.loading_scenarios.CurrentValue,
            token => Task.FromResult(scenarioService.GetScenarios(Route.Model, token)),
            ShowScenarios);
    }

    protected override void OnDeactivated()
    {
        _imageLoader.Cancel();
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
        launcher.OpenDirectory(SelectedScenario!.Scenario.BrowsableDirectoryPath);
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

        await Loading.RunAsync(Strings.replacing_tracks.CurrentValue, _ => trackService.ReplaceTracks(Route.Model, request));
    }

    [RelayCommand]
    private Task CheckAssets()
    {
        var dialog = dialogs.Create<CheckAssetsViewModel>(Route.Model);

        return dialogs.Show(dialog);
    }

    private void LoadLocomotiveImages(List<ScenarioRowViewModel> rows)
    {
        _imageLoader.Load(rows, ReadLocomotiveImage, (row, image) => row.LocomotiveImage = image);
    }

    private Bitmap? ReadLocomotiveImage(ScenarioRowViewModel row)
    {
        var hasPlayerConsist = row.Scenario.PlayerConsist is { BlueprintId.Length: > 0 };

        return hasPlayerConsist ? images.GetBlueprintImage(row.Scenario.PlayerConsist) : null;
    }
}
