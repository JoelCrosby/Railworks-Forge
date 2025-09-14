using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;

using CommunityToolkit.Mvvm.ComponentModel;

using Echoes;

using RailworksForge.Core;
using RailworksForge.Core.Config;
using RailworksForge.Core.External;
using RailworksForge.Core.Packaging;
using RailworksForge.Util;
using RailworksForge.ViewModels.MenuItems;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ToolbarViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _selectedLanguage;

    [ObservableProperty]
    public ObservableCollection<CultureMenuItem> _cultures = [];

    public ToolbarViewModel()
    {
        UpdateSelectedCulture();

        ChangeLanguageCommand = ReactiveCommand.Create<CultureMenuItem>((item) =>
        {
            TranslationProvider.SetCulture(CultureInfo.GetCultureInfo(item.Culture));

            Configuration.Set(Configuration.Get() with
            {
                Language = item.Culture,
            });

            UpdateSelectedCulture();
        });
    }

    public ReactiveCommand<Unit, Unit> ConvertBinToXmlCommand { get; } = ReactiveCommand.CreateFromTask(async (token) =>
    {
        var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

        if (storageFile is null) return;

        var path = storageFile.Path.LocalPath;
        var extension = Path.GetExtension(path);

        if (extension is not ".bin") return;

        var result = await Serz.Convert(path, token, true);

        File.Copy(result.OutputPath, path.Replace(extension, $"{extension}.xml"));
    });

    public ReactiveCommand<Unit, Unit> ConvertXmlToBinCommand { get; } = ReactiveCommand.CreateFromTask(async (token) =>
    {
        var storageFile = await Utils.OpenFilePickerAsync("Select .bin file");

        if (storageFile is null) return;

        var path = storageFile.Path.LocalPath;
        var extension = Path.GetExtension(path);

        if (extension is not ".xml") return;

        var result = await Serz.Convert(path, token, true);

        File.Copy(result.OutputPath, path.Replace($"{extension}.xml", extension));
    });

    public ReactiveCommand<Unit, Unit> OpenSettingsDirectoryCommand { get; } = ReactiveCommand.Create(() =>
    {
        Launcher.Open(Paths.GetConfigurationFolder());
    });

    public ReactiveCommand<Unit, Unit> InstallPackageCommand { get; } = ReactiveCommand.CreateFromTask(async () =>
    {
        var files = await Utils.OpenMultiFilePickerAsync("Select .rwp or .rpk file");

        if (files.Count is 0) return;

        var packager = new Packager();
        var mainWindow = Utils.GetApplicationViewModel();

        packager.PackageInstallProgressSubject.Subscribe(args =>
        {
            mainWindow.UpdateProgressIndicator(args);
        });

        foreach (var file in files)
        {
            await packager.InstallPackage(file.Path.LocalPath);
        }

        mainWindow.ClearProgressIndicator();
    });

    public ReactiveCommand<Unit, Unit> SettingsClickedCommand { get; } = ReactiveCommand.Create(() =>
    {
        Utils.GetApplicationViewModel().SelectAllRoutes();
    });

    public ReactiveCommand<CultureMenuItem, Unit> ChangeLanguageCommand { get; }

    private void UpdateSelectedCulture()
    {
        var currentCulture = Configuration.Get().Language;
        var cultures = new ObservableCollection<CultureMenuItem>
        {
            new (Utils.GetTranslation("english"), "en-GB"),
            new (Utils.GetTranslation("german"), "de-DE"),
        };

        SelectedLanguage = cultures.FirstOrDefault(l => l.Culture == currentCulture)?.Header;
        Cultures = cultures;
    }
}
