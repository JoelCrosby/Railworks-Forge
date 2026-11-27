using RailworksForge.Core.Common;
using RailworksForge.Core.Routes;
using RailworksForge.Core.Scenarios;
using RailworksForge.Tests.Support;
using RailworksForge.Translations;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

[Collection(GlobalStateCollection.Name)]
public class EditDetailsViewModelTests
{
    private readonly ScenarioDetails _details = TestData.ScenarioDetails();

    [Fact]
    public void Scenario_OpensOnTheFirstLanguageWithText()
    {
        var dialog = new EditScenarioDetailsViewModel(_details with { Name = LocalisedText.From((GameLanguage.German, "Nach Glasgow")) });

        Assert.Equal(GameLanguage.German, dialog.SelectedLanguage);
        Assert.Equal("Nach Glasgow", dialog.Name);
        Assert.True(dialog.Languages.Options.Single(option => option.Language is GameLanguage.German).IsSelected);
    }

    [Fact]
    public void Scenario_ApplyNeedsAChange()
    {
        var dialog = new EditScenarioDetailsViewModel(_details);

        Assert.False(dialog.ApplyCommand.CanExecute(null));

        dialog.DurationMinutes = 75;

        Assert.True(dialog.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public void Scenario_KeepsTextPerLanguage()
    {
        var dialog = new EditScenarioDetailsViewModel(_details);

        dialog.SelectLanguageCommand.Execute(GameLanguage.French);
        dialog.Name = "1A01 Euston - Glasgow (FR)";
        dialog.SelectLanguageCommand.Execute(GameLanguage.English);

        Assert.Equal("1A01 Euston - Glasgow", dialog.Name);
        Assert.Equal("1A01 Euston - Glasgow (FR)", dialog.Edited.Name[GameLanguage.French]);
        Assert.True(dialog.Languages.Options.Single(option => option.Language is GameLanguage.French).HasText);
        Assert.False(dialog.Languages.Options.Single(option => option.Language is GameLanguage.Polish).HasText);
    }

    [Fact]
    public void Scenario_ShowsWhatTheGameFallsBackToForAnEmptyLanguage()
    {
        var dialog = new EditScenarioDetailsViewModel(_details);

        Assert.Null(dialog.NameFallback);

        dialog.SelectLanguageCommand.Execute(GameLanguage.Polish);

        Assert.Equal(string.Format(Strings.details_language_fallback.CurrentValue, "1A01 Euston - Glasgow"), dialog.NameFallback);
    }

    [Fact]
    public void Scenario_NeedsANameInSomeLanguage()
    {
        var dialog = new EditScenarioDetailsViewModel(_details with { Name = LocalisedText.From((GameLanguage.English, "1A01")) });

        dialog.Name = "  ";

        Assert.False(dialog.HasName);
        Assert.False(dialog.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public void Scenario_FlagsAndClearsTheEditorPlaceholder()
    {
        var dialog = new EditScenarioDetailsViewModel(_details);

        Assert.True(dialog.IsDescriptionPlaceholder);
        Assert.False(dialog.IsBriefingPlaceholder);

        dialog.ClearDescriptionCommand.Execute(null);

        Assert.False(dialog.IsDescriptionPlaceholder);
        Assert.False(dialog.Edited.Description.HasAnyText);
    }

    [Fact]
    public void Scenario_ApplyReturnsEveryEditedField()
    {
        var dialog = new EditScenarioDetailsViewModel(_details);
        ScenarioDetails? result = null;
        dialog.CloseRequested += value => result = value as ScenarioDetails;

        dialog.StartLocation = "Euston platform 15";
        dialog.SelectedRating = dialog.RatingOptions.Single(option => option.Value == 2);
        dialog.SelectedSeason = dialog.SeasonOptions.Single(option => option.Season is ScenarioSeason.Winter);
        dialog.StartDate = new DateTime(1985, 12, 24);
        dialog.StartTime = new TimeSpan(6, 30, 15);
        dialog.ApplyCommand.Execute(null);

        Assert.NotNull(result);
        Assert.Equal("Euston platform 15", result.StartLocation[GameLanguage.English]);
        Assert.Equal(2, result.Rating);
        Assert.Equal(ScenarioSeason.Winter, result.Season);
        Assert.Equal(new DateOnly(1985, 12, 24), result.StartDate);
        Assert.Equal(new TimeSpan(6, 30, 15), result.StartTime);
        Assert.Equal(50, result.DurationMinutes);
    }

    [Fact]
    public void Scenario_KeepsAnUnsetRatingAndSeason()
    {
        var unset = _details with { Rating = 0, Season = ScenarioSeason.None };
        var dialog = new EditScenarioDetailsViewModel(unset);

        Assert.Null(dialog.SelectedRating);
        Assert.Null(dialog.SelectedSeason);
        Assert.False(dialog.HasChanges);
    }

    [Fact]
    public void Route_SavesTheRenamedRoute()
    {
        var details = new RouteDetails { Name = LocalisedText.From((GameLanguage.English, "Tirol Austria")), IsLocked = true };
        var dialog = new EditRouteDetailsViewModel("route-id", details);
        RouteDetails? result = null;
        dialog.CloseRequested += value => result = value as RouteDetails;

        Assert.False(dialog.SaveCommand.CanExecute(null));
        Assert.True(dialog.IsLocked);

        dialog.SelectLanguageCommand.Execute(GameLanguage.German);
        dialog.Name = "Tirol Österreich";
        dialog.SaveCommand.Execute(null);

        Assert.NotNull(result);
        Assert.Equal("Tirol Österreich", result.Name[GameLanguage.German]);
        Assert.Equal("Tirol Austria", result.Name[GameLanguage.English]);
    }

    [Fact]
    public void Route_NeedsAName()
    {
        var details = new RouteDetails { Name = LocalisedText.From((GameLanguage.English, "Tirol Austria")) };
        var dialog = new EditRouteDetailsViewModel("route-id", details);

        dialog.Name = string.Empty;

        Assert.False(dialog.HasName);
        Assert.False(dialog.SaveCommand.CanExecute(null));
    }
}
