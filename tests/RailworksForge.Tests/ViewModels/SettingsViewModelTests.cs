using Echoes;

using NSubstitute;

using RailworksForge.Core.Config;
using RailworksForge.Services;
using RailworksForge.Tests.Support;
using RailworksForge.ViewModels;

namespace RailworksForge.Tests.ViewModels;

[Collection(GlobalStateCollection.Name)]
public sealed class SettingsViewModelTests : IDisposable
{
    private readonly System.Globalization.CultureInfo _previousCulture = TranslationProvider.Culture;
    private readonly IThemeService _themes = Substitute.For<IThemeService>();
    private readonly ILauncherService _launcher = Substitute.For<ILauncherService>();
    private readonly IStoragePickerService _picker = Substitute.For<IStoragePickerService>();
    private readonly IAppLifetimeService _lifetime = Substitute.For<IAppLifetimeService>();
    private readonly ISettingsService _settings = Substitute.For<ISettingsService>();
    private readonly IGameDirectoryService _gameDirectory = Substitute.For<IGameDirectoryService>();
    private readonly AppDirectories _appDirectories = new();

    private ConfigurationOptions _config = new()
    {
        Language = "en-GB",
        Theme = "Dark",
        UseInternalSerz = true,
    };

    public SettingsViewModelTests()
    {
        _settings.Current.Returns(_ => _config);
        _settings
            .When(settings => settings.Update(Arg.Any<Func<ConfigurationOptions, ConfigurationOptions>>()))
            .Do(call => _config = call.Arg<Func<ConfigurationOptions, ConfigurationOptions>>().Invoke(_config));
        _gameDirectory.Current.Returns("/games/RailWorks");
        _gameDirectory.IsValid("/games/RailWorks").Returns(true);
    }

    public void Dispose()
    {
        TranslationProvider.SetCulture(_previousCulture);
    }

    [Fact]
    public void Opening_ShowsTheCurrentSettingsWithoutSavingThem()
    {
        var settings = CreateSettings();

        Assert.Equal("/games/RailWorks", settings.GameDirectoryPath);
        Assert.Equal("en-GB", settings.SelectedLanguage?.Key);
        Assert.Equal("Dark", settings.SelectedTheme?.Key);
        Assert.True(settings.UseInternalSerz);
        Assert.True(settings.IsGameDirectoryValid);
        _settings.DidNotReceiveWithAnyArgs().Update(default!);
        _themes.DidNotReceiveWithAnyArgs().Apply(default!);
    }

    [Fact]
    public void Opening_WithAnUnsupportedLanguage_SwitchesToEnglish()
    {
        _config = _config with { Language = "pt-BR" };

        var settings = CreateSettings();

        Assert.Equal("en-GB", settings.SelectedLanguage?.Key);
        Assert.Equal("en-GB", _config.Language);
    }

    [Fact]
    public void SelectingATheme_AppliesAndSavesIt()
    {
        var settings = CreateSettings();

        settings.SelectedTheme = settings.Themes.Single(theme => theme.Key == "Light");

        _themes.Received(1).Apply("Light");
        Assert.Equal("Light", _config.Theme);
    }

    [Fact]
    public void SelectingALanguage_SwitchesTheCultureAndKeepsTheTheme()
    {
        var settings = CreateSettings();
        var previousThemes = settings.Themes;

        settings.SelectedLanguage = settings.Languages.Single(language => language.Key == "fr-FR");

        Assert.Equal("fr-FR", TranslationProvider.Culture.Name);
        Assert.Equal("fr-FR", _config.Language);
        Assert.NotSame(previousThemes, settings.Themes);
        Assert.Equal("Dark", settings.SelectedTheme?.Key);
        _themes.DidNotReceiveWithAnyArgs().Apply(default!);
    }

    [Fact]
    public void TogglingInternalSerz_SavesIt()
    {
        var settings = CreateSettings();

        settings.UseInternalSerz = false;

        Assert.False(_config.UseInternalSerz);
    }

    [Fact]
    public async Task BrowseGameDirectory_Cancelled_KeepsTheCurrentDirectory()
    {
        var settings = CreateSettings();
        _picker.PickFolder(Arg.Any<string>()).Returns((string?)null);

        await settings.BrowseGameDirectoryCommand.ExecuteAsync(null);

        Assert.Equal("/games/RailWorks", settings.GameDirectoryPath);
        _gameDirectory.DidNotReceiveWithAnyArgs().Change(default!);
    }

    [Fact]
    public async Task BrowseGameDirectory_ToAnotherInstall_AsksForARestart()
    {
        var settings = CreateSettings();
        _picker.PickFolder(Arg.Any<string>()).Returns("/mnt/RailWorks");
        _gameDirectory.IsValid("/mnt/RailWorks").Returns(true);
        _gameDirectory.Change("/mnt/RailWorks").Returns(GameDirectoryChange.RestartRequired);

        await settings.BrowseGameDirectoryCommand.ExecuteAsync(null);

        Assert.Equal("/mnt/RailWorks", settings.GameDirectoryPath);
        Assert.True(settings.IsRestartRequired);
        Assert.False(settings.IsGameDirectoryInvalid);
    }

    [Fact]
    public async Task BrowseGameDirectory_ToTheCurrentInstall_NeedsNoRestart()
    {
        var settings = CreateSettings();
        _picker.PickFolder(Arg.Any<string>()).Returns("/games/RailWorks/");
        _gameDirectory.Change("/games/RailWorks/").Returns(GameDirectoryChange.Saved);

        await settings.BrowseGameDirectoryCommand.ExecuteAsync(null);

        Assert.False(settings.IsRestartRequired);
    }

    [Fact]
    public async Task BrowseGameDirectory_ToAnInvalidFolder_ShowsItAsInvalid()
    {
        var settings = CreateSettings();
        _picker.PickFolder(Arg.Any<string>()).Returns("/home/user/Downloads");
        _gameDirectory.Change("/home/user/Downloads").Returns(GameDirectoryChange.Invalid);

        await settings.BrowseGameDirectoryCommand.ExecuteAsync(null);

        Assert.Equal("/home/user/Downloads", settings.GameDirectoryPath);
        Assert.True(settings.IsGameDirectoryInvalid);
        Assert.False(settings.IsRestartRequired);
    }

    [Fact]
    public void ResetTableSorting_ForgetsEverySavedColumnSort()
    {
        _config.DataGrids["routes"] = new DataGridOptions { SortingColumn = "Name" };
        var settings = CreateSettings();

        settings.ResetTableSortingCommand.Execute(null);

        Assert.Empty(_config.DataGrids);
        Assert.True(settings.IsTableSortingReset);
    }

    [Fact]
    public void FolderCommands_OpenTheMatchingFolders()
    {
        var settings = CreateSettings();

        settings.OpenGameDirectoryCommand.Execute(null);
        settings.OpenSettingsFolderCommand.Execute(null);
        settings.OpenLogsFolderCommand.Execute(null);
        settings.OpenCacheFolderCommand.Execute(null);

        _launcher.Received(1).OpenDirectory("/games/RailWorks");
        _launcher.Received(1).OpenOrCreateDirectory(_appDirectories.Configuration);
        _launcher.Received(1).OpenOrCreateDirectory(_appDirectories.Logs);
        _launcher.Received(1).OpenOrCreateDirectory(_appDirectories.Cache);
    }

    [Fact]
    public void Restart_RestartsTheApp()
    {
        var settings = CreateSettings();

        settings.RestartCommand.Execute(null);

        _lifetime.Received(1).Restart();
    }

    private SettingsViewModel CreateSettings()
    {
        return new SettingsViewModel(_themes, _launcher, _picker, _lifetime, _settings, _gameDirectory, _appDirectories);
    }
}
