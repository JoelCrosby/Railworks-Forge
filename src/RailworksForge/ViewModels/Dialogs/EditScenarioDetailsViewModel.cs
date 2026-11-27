using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core.Common;
using RailworksForge.Core.Scenarios;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public sealed record RatingOption(int Value, string Label);

public sealed record SeasonOption(ScenarioSeason Season, string Label);

public partial class EditScenarioDetailsViewModel : DialogViewModel<ScenarioDetails>
{
    private const int MaxRating = 5;

    private readonly ScenarioDetails _original;

    public EditScenarioDetailsViewModel(ScenarioDetails details)
    {
        _original = details;
        Edited = details;
        RatingOptions = Enumerable.Range(1, MaxRating).Select(CreateRatingOption).ToList();
        SeasonOptions = CreateSeasonOptions();
        SelectedRating = RatingOptions.FirstOrDefault(option => option.Value == details.Rating);
        SelectedSeason = SeasonOptions.FirstOrDefault(option => option.Season == details.Season);
        DurationMinutes = details.DurationMinutes;
        StartDate = details.StartDate?.ToDateTime(TimeOnly.MinValue);
        StartTime = details.StartTime;
        SelectedLanguage = details.Name.FirstLanguageWithText ?? GameLanguage.English;
        ShowLanguage(SelectedLanguage);
    }

    public LanguageChoices Languages { get; } = new();

    public IReadOnlyList<RatingOption> RatingOptions { get; }

    public IReadOnlyList<SeasonOption> SeasonOptions { get; }

    public ScenarioClass ScenarioClass => _original.ScenarioClass;

    public bool IsWorkshopItem => _original.IsWorkshopItem;

    // Holds the text for every language; the fields below show the selected language's part of it.
    [ObservableProperty]
    public partial ScenarioDetails Edited { get; private set; }

    [ObservableProperty]
    public partial GameLanguage SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartLocation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Briefing { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial decimal? DurationMinutes { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial RatingOption? SelectedRating { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial SeasonOption? SelectedSeason { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial DateTime? StartDate { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial TimeSpan? StartTime { get; set; }

    public string? NameFallback => FallbackFor(Edited.Name);

    public string? StartLocationFallback => FallbackFor(Edited.StartLocation);

    public bool IsDescriptionPlaceholder => IsEditorPlaceholder(Description);

    public bool IsBriefingPlaceholder => IsEditorPlaceholder(Briefing);

    public bool HasName => Edited.Name.HasAnyText;

    public bool HasChanges => BuildResult() != _original;

    private bool CanApply => HasChanges && HasName && DurationMinutes is not null;

    [RelayCommand]
    private void SelectLanguage(GameLanguage language)
    {
        SelectedLanguage = language;
    }

    [RelayCommand]
    private void ClearDescription()
    {
        Description = string.Empty;
    }

    [RelayCommand]
    private void ClearBriefing()
    {
        Briefing = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        Close(BuildResult());
    }

    partial void OnSelectedLanguageChanged(GameLanguage value)
    {
        ShowLanguage(value);
    }

    private void ShowLanguage(GameLanguage language)
    {
        Languages.Select(language);
        Name = Edited.Name[language];
        StartLocation = Edited.StartLocation[language];
        Description = Edited.Description[language];
        Briefing = Edited.Briefing[language];
        OnPropertyChanged(nameof(NameFallback));
        OnPropertyChanged(nameof(StartLocationFallback));
    }

    partial void OnNameChanged(string value)
    {
        Edited = Edited with { Name = Edited.Name.With(SelectedLanguage, value) };
    }

    partial void OnStartLocationChanged(string value)
    {
        Edited = Edited with { StartLocation = Edited.StartLocation.With(SelectedLanguage, value) };
    }

    partial void OnDescriptionChanged(string value)
    {
        Edited = Edited with { Description = Edited.Description.With(SelectedLanguage, value) };
        OnPropertyChanged(nameof(IsDescriptionPlaceholder));
    }

    partial void OnBriefingChanged(string value)
    {
        Edited = Edited with { Briefing = Edited.Briefing.With(SelectedLanguage, value) };
        OnPropertyChanged(nameof(IsBriefingPlaceholder));
    }

    partial void OnEditedChanged(ScenarioDetails value)
    {
        Languages.MarkText(language => HasTextIn(value, language));
        OnPropertyChanged(nameof(NameFallback));
        OnPropertyChanged(nameof(StartLocationFallback));
        OnPropertyChanged(nameof(HasName));
        ApplyCommand.NotifyCanExecuteChanged();
    }

    private ScenarioDetails BuildResult()
    {
        var duration = DurationMinutes is { } minutes ? RoundMinutes(minutes) : _original.DurationMinutes;
        var startDate = StartDate is { } date ? DateOnly.FromDateTime(date) : _original.StartDate;

        return Edited with
        {
            DurationMinutes = duration,
            Rating = SelectedRating?.Value ?? _original.Rating,
            Season = SelectedSeason?.Season ?? _original.Season,
            StartDate = startDate,
            StartTime = StartTime ?? _original.StartTime,
        };
    }

    // Rounded the way the duration box displays it, so a typed 89.6 is saved as the 90 shown.
    private static int RoundMinutes(decimal minutes)
    {
        return (int)Math.Round(minutes, MidpointRounding.AwayFromZero);
    }

    // Only offered while the selected language is empty, since that is when the game shows another language.
    private string? FallbackFor(LocalisedText text)
    {
        var isEmptyHere = !text.HasText(SelectedLanguage);
        var showsAnotherLanguage = isEmptyHere && text.HasAnyText;

        return showsAnotherLanguage ? string.Format(Strings.details_language_fallback.CurrentValue, text.DisplayText) : null;
    }

    private static bool HasTextIn(ScenarioDetails details, GameLanguage language)
    {
        LocalisedText[] texts = [details.Name, details.StartLocation, details.Description, details.Briefing];

        return texts.Any(text => text.HasText(language));
    }

    // The scenario editor fills new scenarios with text such as "<supply description>".
    private static bool IsEditorPlaceholder(string text)
    {
        return EditorPlaceholderPattern().IsMatch(text.Trim());
    }

    private static RatingOption CreateRatingOption(int rating)
    {
        var stars = new string('★', rating) + new string('☆', MaxRating - rating);

        return new RatingOption(rating, stars);
    }

    // A scenario without a season shows none selected and keeps it unless one is picked.
    private static List<SeasonOption> CreateSeasonOptions()
    {
        List<SeasonOption> seasons =
        [
            new(ScenarioSeason.Spring, Strings.season_spring.CurrentValue),
            new(ScenarioSeason.Summer, Strings.season_summer.CurrentValue),
            new(ScenarioSeason.Autumn, Strings.season_autumn.CurrentValue),
            new(ScenarioSeason.Winter, Strings.season_winter.CurrentValue),
        ];

        return seasons;
    }

    [GeneratedRegex(@"^<supply [a-z ]+>$", RegexOptions.IgnoreCase)]
    private static partial Regex EditorPlaceholderPattern();
}
