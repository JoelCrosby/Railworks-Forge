using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Services;
using RailworksForge.Util;

using Serilog;

namespace RailworksForge.ViewModels;

public partial class RouteDetailViewModel(
    RouteViewModel route,
    ScenarioService scenarioService,
    TrackService trackService,
    NavigationService navigation,
    DialogService dialogs,
    LauncherService launcher,
    ClipboardService clipboard) : ViewModelBase
{
    private CancellationTokenSource? _imageLoading;

    public RouteViewModel Route { get; } = route;

    public SearchableCollection<Scenario> Scenarios { get; } = new(scenario => scenario.SearchIndex);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyScenarioNameCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenScenarioInExplorerCommand))]
    public partial Scenario? SelectedScenario { get; set; }

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
        Scenarios.Reset(scenarios);
        LoadLocomotiveImages(scenarios);
    }

    private void OnPlayerInfoUpdated()
    {
        Dispatcher.UIThread.Post(() => scenarioService.RefreshPlayerInfo(Scenarios.Source));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedScenario))]
    private void OpenScenario()
    {
        navigation.ShowScenario(SelectedScenario!);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedScenario))]
    private Task CopyScenarioName()
    {
        return clipboard.SetText(SelectedScenario!.Name);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedScenario))]
    private void OpenScenarioInExplorer()
    {
        launcher.OpenDirectory(SelectedScenario!.DirectoryPath);
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
    private void LoadLocomotiveImages(List<Scenario> scenarios)
    {
        _imageLoading?.Cancel();
        var cancellation = new CancellationTokenSource();
        _imageLoading = cancellation;
        var token = cancellation.Token;

        _ = Task.Run(() =>
        {
            // Many scenarios share a locomotive, and a failed lookup searches archives, so remember misses too.
            var imagesByBlueprint = new Dictionary<string, Bitmap?>(StringComparer.OrdinalIgnoreCase);

            foreach (var scenario in scenarios)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (scenario.PlayerConsist is not { BlueprintId.Length: > 0 } playerConsist)
                {
                    continue;
                }

                var image = GetLocomotiveImage(playerConsist, imagesByBlueprint);

                if (image is not null)
                {
                    Dispatcher.UIThread.Post(() => scenario.LocomotiveImage = image);
                }
            }
        }, token);
    }

    private static Bitmap? GetLocomotiveImage(Consist consist, Dictionary<string, Bitmap?> imagesByBlueprint)
    {
        if (imagesByBlueprint.TryGetValue(consist.BinaryPath, out var cached))
        {
            return cached;
        }

        try
        {
            var image = BitmapUtils.GetImageBitmap(consist);
            imagesByBlueprint[consist.BinaryPath] = image;

            return image;
        }
        catch (Exception e)
        {
            Log.Debug(e, "failed to load locomotive image for {BlueprintId}", consist.BlueprintId);
            imagesByBlueprint[consist.BinaryPath] = null;

            return null;
        }
    }
}
