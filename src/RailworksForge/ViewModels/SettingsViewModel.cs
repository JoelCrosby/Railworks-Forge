using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;

using CommunityToolkit.Mvvm.ComponentModel;

using Echoes;

using RailworksForge.Core;
using RailworksForge.Core.Config;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public record SettingsOption(string Key, string Header);

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGameDirectorySet))]
    [NotifyPropertyChangedFor(nameof(IsGameDirectoryValid))]
    [NotifyPropertyChangedFor(nameof(IsGameDirectoryInvalid))]
    private string _gameDirectoryPath;

    [ObservableProperty]
    private bool _isRestartRequired;

    [ObservableProperty]
    private IReadOnlyList<SettingsOption> _themes = [];

    [ObservableProperty]
    private SettingsOption? _selectedTheme;

    [ObservableProperty]
    private SettingsOption? _selectedLanguage;

    [ObservableProperty]
    private bool _useInternalSerz;

    [ObservableProperty]
    private bool _isTableSortingReset;

    public IReadOnlyList<SettingsOption> Languages { get; } =
    [
        new ("en-GB", "English"),
        new ("de-DE", "Deutsch"),
    ];

    public bool IsGameDirectorySet => !string.IsNullOrWhiteSpace(GameDirectoryPath);

    public bool IsGameDirectoryValid => Paths.IsValidGameDirectory(GameDirectoryPath);

    public bool IsGameDirectoryInvalid => IsGameDirectorySet && !IsGameDirectoryValid;

    public ReactiveCommand<Unit, Unit> BrowseGameDirectoryCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenGameDirectoryCommand { get; }
    public ReactiveCommand<Unit, Unit> RestartCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetTableSortingCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLogsFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCacheFolderCommand { get; }

    public SettingsViewModel()
    {
        var config = Configuration.Get();

        _gameDirectoryPath = Paths.GetGameDirectory();
        _useInternalSerz = config.UseInternalSerz;
        _selectedLanguage = Languages.FirstOrDefault(language => language.Key == config.Language) ?? Languages[0];

        BuildThemes(config.Theme);

        BrowseGameDirectoryCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var folder = await Utils.OpenFolderPickerAsync(Utils.GetTranslation("game_directory"));

            if (folder is null)
            {
                return;
            }

            SetGameDirectory(folder.Path.LocalPath);
        });

        OpenGameDirectoryCommand = ReactiveCommand.Create(() => Launcher.Open(GameDirectoryPath));
        RestartCommand = ReactiveCommand.Create(Utils.RestartApplication);

        ResetTableSortingCommand = ReactiveCommand.Create(() =>
        {
            var current = Configuration.Get();
            current.DataGrids.Clear();
            Configuration.Set(current);
            IsTableSortingReset = true;
        });

        OpenSettingsFolderCommand = ReactiveCommand.Create(() => OpenFolder(Paths.GetConfigurationFolder()));
        OpenLogsFolderCommand = ReactiveCommand.Create(() => OpenFolder(Paths.GetLoggingPath()));
        OpenCacheFolderCommand = ReactiveCommand.Create(() => OpenFolder(Paths.GetCacheFolder()));
    }

    partial void OnSelectedThemeChanged(SettingsOption? value)
    {
        var isNewTheme = value is not null && value.Key != Configuration.Get().Theme;

        if (!isNewTheme)
        {
            return;
        }

        App.ApplyTheme(value!.Key);
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
        Configuration.Set(Configuration.Get() with { UseInternalSerz = value });
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

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Launcher.Open(path);
    }
}
