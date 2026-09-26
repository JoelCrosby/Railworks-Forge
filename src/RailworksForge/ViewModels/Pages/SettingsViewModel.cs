using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Echoes;

using RailworksForge.Core;
using RailworksForge.Core.Config;
using RailworksForge.Services;
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
    ];

    public bool IsGameDirectorySet => !string.IsNullOrWhiteSpace(GameDirectoryPath);

    public bool IsGameDirectoryValid => Paths.IsValidGameDirectory(GameDirectoryPath);

    public bool IsGameDirectoryInvalid => IsGameDirectorySet && !IsGameDirectoryValid;

    private readonly ThemeService _themeService;
    private readonly LauncherService _launcher;
    private readonly StoragePickerService _picker;
    private readonly AppLifetimeService _lifetime;

    public SettingsViewModel(
        ThemeService themes,
        LauncherService launcher,
        StoragePickerService picker,
        AppLifetimeService lifetime)
    {
        _themeService = themes;
        _launcher = launcher;
        _picker = picker;
        _lifetime = lifetime;

        var config = Configuration.Get();

        GameDirectoryPath = Paths.GetGameDirectory();
        UseInternalSerz = config.UseInternalSerz;
        SelectedLanguage = Languages.FirstOrDefault(language => language.Key == config.Language) ?? Languages[0];

        BuildThemes(config.Theme);
    }

    [RelayCommand]
    private async Task BrowseGameDirectory()
    {
        var folder = await _picker.PickFolder(Utils.GetTranslation("game_directory"));

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
        var current = Configuration.Get();
        current.DataGrids.Clear();
        Configuration.Set(current);
        IsTableSortingReset = true;
    }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        _launcher.OpenOrCreateDirectory(Paths.GetConfigurationFolder());
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        _launcher.OpenOrCreateDirectory(Paths.GetLoggingPath());
    }

    [RelayCommand]
    private void OpenCacheFolder()
    {
        _launcher.OpenOrCreateDirectory(Paths.GetCacheFolder());
    }

    partial void OnSelectedThemeChanged(SettingsOption? value)
    {
        var isNewTheme = value is not null && value.Key != Configuration.Get().Theme;

        if (!isNewTheme)
        {
            return;
        }

        _themeService.Apply(value!.Key);
        Configuration.Set(Configuration.Get() with { Theme = value.Key });
    }

    partial void OnSelectedLanguageChanged(SettingsOption? value)
    {
        var isNewLanguage = value is not null && value.Key != Configuration.Get().Language;

        if (!isNewLanguage)
        {
            return;
        }

        TranslationProvider.SetCulture(CultureInfo.GetCultureInfo(value!.Key));
        Configuration.Set(Configuration.Get() with { Language = value.Key });

        // The theme names come from translations, so rebuild them in the new language.
        BuildThemes(Configuration.Get().Theme);
    }

    partial void OnUseInternalSerzChanged(bool value)
    {
        var config = Configuration.Get();

        if (value == config.UseInternalSerz)
        {
            return;
        }

        Configuration.Set(config with { UseInternalSerz = value });
    }

    private void SetGameDirectory(string path)
    {
        GameDirectoryPath = path;

        if (!IsGameDirectoryValid)
        {
            return;
        }

        Configuration.Set(Configuration.Get() with { GameDirectoryPath = path });

        var runningDirectory = Paths.GetGameDirectory();
        var isRunningDirectorySet = !string.IsNullOrWhiteSpace(runningDirectory);
        var isRunningDirectory = isRunningDirectorySet && IsSamePath(path, runningDirectory);

        IsRestartRequired = !isRunningDirectory;
    }

    private static bool IsSamePath(string first, string second)
    {
        var normalisedFirst = Path.TrimEndingDirectorySeparator(Path.GetFullPath(first));
        var normalisedSecond = Path.TrimEndingDirectorySeparator(Path.GetFullPath(second));

        return normalisedFirst == normalisedSecond;
    }

    private void BuildThemes(string selectedKey)
    {
        Themes =
        [
            new ("System", Utils.GetTranslation("system")),
            new ("Light", Utils.GetTranslation("light")),
            new ("Dark", Utils.GetTranslation("dark")),
        ];

        SelectedTheme = Themes.FirstOrDefault(theme => theme.Key == selectedKey) ?? Themes[0];
    }
}
