using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Echoes;

using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;
using RailworksForge.Services;
using RailworksForge.Translations;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class RouteDetailViewModel(
    RouteViewModel route,
    IScenarioService scenarioService,
    ITrackService trackService,
    IImageService images,
    INavigationService navigation,
    IDialogService dialogs,
    ILauncherService launcher,
    IClipboardService clipboard,
    IDispatcher dispatcher) : ViewModelBase
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchPlaceholder))]
    public partial int ScenarioCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompletedText))]
    public partial int CompletedCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingAll))]
    [NotifyPropertyChangedFor(nameof(IsShowingNotCompleted))]
    [NotifyPropertyChangedFor(nameof(IsShowingCompleted))]
    public partial CompletionFilter Completion { get; set; }

    [ObservableProperty]
    public partial string? Season { get; set; }

    public string SearchPlaceholder => string.Format(Strings.search_scenarios.CurrentValue, ScenarioCount);

    public string ScenarioCountText => string.Format(Strings.scenario_count.CurrentValue, ScenarioCount);

    public string CompletedText => string.Format(Strings.completed_count.CurrentValue, CompletedCount);

    public bool IsShowingAll => Completion is CompletionFilter.All;

    public bool IsShowingNotCompleted => Completion is CompletionFilter.NotCompleted;

    public bool IsShowingCompleted => Completion is CompletionFilter.Completed;

    public bool ShowsNoMatches => Scenarios.Count == 0 && ScenarioCount > 0;

    public override string StatusText => string.Format(Strings.scenario_count.CurrentValue, Scenarios.Count);

    private bool HasSelectedScenario => SelectedScenario is not null;

    protected override Task OnActivated()
    {
        scenarioService.PlayerInfoUpdated -= OnPlayerInfoUpdated;
        scenarioService.PlayerInfoUpdated += OnPlayerInfoUpdated;
        TranslationProvider.OnCultureChanged -= OnCultureChanged;
        TranslationProvider.OnCultureChanged += OnCultureChanged;

        return Loading.RunAsync(
            Strings.loading_scenarios.CurrentValue,
            token => Task.FromResult(scenarioService.GetScenarios(Route.Model, token)),
            ShowScenarios);
    }

    protected override void OnDeactivated()
    {
        _imageLoader.Cancel();
        scenarioService.PlayerInfoUpdated -= OnPlayerInfoUpdated;
        TranslationProvider.OnCultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(ScenarioCountText));
        OnPropertyChanged(nameof(CompletedText));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnSearchTermChanged(string? value)
    {
        Scenarios.Filter(value);
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ShowsNoMatches));
    }

    partial void OnScenarioCountChanged(int value)
    {
        OnPropertyChanged(nameof(ScenarioCountText));
    }

    partial void OnCompletionChanged(CompletionFilter value)
    {
        ApplyFilters();
    }

    partial void OnSeasonChanged(string? value)
    {
        ApplyFilters();
    }

    [RelayCommand]
    private void ShowCompletion(CompletionFilter filter)
    {
        Completion = filter;
    }

    [RelayCommand]
    private void ToggleSeason(string season)
    {
        var isCurrentSeason = Season == season;

        Season = isCurrentSeason ? null : season;
    }

    private void ApplyFilters()
    {
        var completion = Completion;
        var season = Season;

        bool Matches(ScenarioRowViewModel row)
        {
            var matchesCompletion = MatchesCompletion(row, completion);
            var matchesSeason = MatchesSeason(row, season);

            return matchesCompletion && matchesSeason;
        }

        Scenarios.Where(Matches);
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ShowsNoMatches));
    }

    private static bool MatchesCompletion(ScenarioRowViewModel row, CompletionFilter filter)
    {
        var isCompleted = row.IsCompleted;

        return filter switch
        {
            CompletionFilter.Completed => isCompleted,
            CompletionFilter.NotCompleted => !isCompleted,
            _ => true,
        };
    }

    private static bool MatchesSeason(ScenarioRowViewModel row, string? season)
    {
        var hasSeasonFilter = season is not null;

        return !hasSeasonFilter || row.Scenario.Season == season;
    }

    private void CountCompleted()
    {
        CompletedCount = Scenarios.Source.Count(row => row.IsCompleted);
    }

    private void ShowScenarios(List<Scenario> scenarios)
    {
        scenarioService.RefreshPlayerInfo(scenarios);

        var rows = scenarios.ConvertAll(scenario => new ScenarioRowViewModel(scenario));

        Scenarios.Reset(rows);
        ScenarioCount = rows.Count;
        CountCompleted();
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ShowsNoMatches));
        LoadLocomotiveImages(rows);
    }

    private void OnPlayerInfoUpdated()
    {
        dispatcher.Post(RefreshPlayerInfo);
    }

    private void RefreshPlayerInfo()
    {
        var scenarios = Scenarios.Source.Select(row => row.Scenario);

        scenarioService.RefreshPlayerInfo(scenarios);

        foreach (var row in Scenarios.Source)
        {
            row.RefreshPlayerInfo();
        }

        CountCompleted();
        ApplyFilters();
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
