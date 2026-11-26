using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Echoes;

using RailworksForge.Core.Config;
using RailworksForge.Services;
using RailworksForge.Translations;
using RailworksForge.Util;

namespace RailworksForge.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGameDirectorySet))]
    [NotifyPropertyChangedFor(nameof(IsGameDirectoryValid))]
    [NotifyPropertyChangedFor(nameof(IsGameDirectoryInvalid))]
    public partial string GameDirectoryPath { get; set; }

    [ObservableProperty]
    public partial bool IsRestartRequired { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<SettingsOption> Themes { get; set; } = [];

    [ObservableProperty]
    public partial SettingsOption? SelectedTheme { get; set; }

    [ObservableProperty]
    public partial SettingsOption? SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial bool UseInternalSerz { get; set; }

    [ObservableProperty]
    public partial bool IsTableSortingReset { get; set; }

    public IReadOnlyList<SettingsOption> Languages { get; } =
    [
        new ("en-GB", "English"),
        new ("de-DE", "Deutsch"),
        new ("es-ES", "Español"),
        new ("fr-FR", "Français"),
    ];

    public bool IsGameDirectorySet => !string.IsNullOrWhiteSpace(GameDirectoryPath);

    public bool IsGameDirectoryValid => _gameDirectory.IsValid(GameDirectoryPath);

    public bool IsGameDirectoryInvalid => IsGameDirectorySet && !IsGameDirectoryValid;

    private readonly ThemeService _themeService;
    private readonly LauncherService _launcher;
    private readonly StoragePickerService _picker;
    private readonly AppLifetimeService _lifetime;
    private readonly SettingsService _settings;
    private readonly GameDirectoryService _gameDirectory;
    private readonly AppDirectories _appDirectories;

    public SettingsViewModel(
        ThemeService themes,
        LauncherService launcher,
        StoragePickerService picker,
        AppLifetimeService lifetime,
        SettingsService settings,
        GameDirectoryService gameDirectory,
        AppDirectories appDirectories)
    {
        _themeService = themes;
        _launcher = launcher;
        _picker = picker;
        _lifetime = lifetime;
        _settings = settings;
        _gameDirectory = gameDirectory;
        _appDirectories = appDirectories;

        var config = settings.Current;

        GameDirectoryPath = gameDirectory.Current;
        UseInternalSerz = config.UseInternalSerz;
        SelectedLanguage = Languages.FirstOrDefault(language => language.Key == config.Language) ?? Languages[0];

        BuildThemes(config.Theme);
    }

    [RelayCommand]
    private async Task BrowseGameDirectory()
    {
        var folder = await _picker.PickFolder(Strings.game_directory.CurrentValue);

        if (folder is null)
        {
            return;
        }

        SetGameDirectory(folder);
    }

    [RelayCommand]
    private void OpenGameDirectory()
    {
        _launcher.OpenDirectory(GameDirectoryPath);
    }

    [RelayCommand]
    private void Restart()
    {
        _lifetime.Restart();
    }

    [RelayCommand]
    private void ResetTableSorting()
    {
        _settings.Update(config => config with { DataGrids = [] });
        IsTableSortingReset = true;
    }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        _launcher.OpenOrCreateDirectory(_appDirectories.Configuration);
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        _launcher.OpenOrCreateDirectory(_appDirectories.Logs);
    }

    [RelayCommand]
    private void OpenCacheFolder()
    {
        _launcher.OpenOrCreateDirectory(_appDirectories.Cache);
    }

    partial void OnSelectedThemeChanged(SettingsOption? value)
    {
        var isNewTheme = value is not null && value.Key != _settings.Current.Theme;

        if (!isNewTheme)
        {
            return;
        }

        _themeService.Apply(value!.Key);
        _settings.Update(config => config with { Theme = value.Key });
    }

    partial void OnSelectedLanguageChanged(SettingsOption? value)
    {
        var isNewLanguage = value is not null && value.Key != _settings.Current.Language;

        if (!isNewLanguage)
        {
            return;
        }

        TranslationProvider.SetCulture(CultureInfo.GetCultureInfo(value!.Key));
        _settings.Update(config => config with { Language = value.Key });

        // The theme names come from translations, so rebuild them in the new language.
        BuildThemes(_settings.Current.Theme);
    }

    partial void OnUseInternalSerzChanged(bool value)
    {
        _settings.Update(config => config with { UseInternalSerz = value });
    }

    private void SetGameDirectory(string path)
    {
        GameDirectoryPath = path;

        var change = _gameDirectory.Change(path);

        if (change is GameDirectoryChange.Invalid)
        {
            return;
        }

        IsRestartRequired = change is GameDirectoryChange.RestartRequired;
    }

    private void BuildThemes(string selectedKey)
    {
        Themes =
        [
            new ("System", Strings.system.CurrentValue),
            new ("Light", Strings.light.CurrentValue),
            new ("Dark", Strings.dark.CurrentValue),
        ];

        SelectedTheme = Themes.FirstOrDefault(theme => theme.Key == selectedKey) ?? Themes[0];
    }
}
