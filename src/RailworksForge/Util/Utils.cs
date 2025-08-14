using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

using Echoes;

using RailworksForge.ViewModels;

namespace RailworksForge.Util;

public class Utils
{
    public static Window GetApplicationWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window;
        }

        throw new Exception("could not get application window");
    }

    public static MainWindowViewModel GetApplicationViewModel()
    {
        var window = GetApplicationWindow();

        if (window.DataContext is MainWindowViewModel context)
        {
            return context;
        }

        throw new Exception("could not get application view model");
    }

    public static async Task<IStorageFile?> OpenFilePickerAsync(string title)
    {
        var window = GetApplicationWindow();
        var provider = window.StorageProvider;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.All],
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public static async Task<List<IStorageFile>> OpenMultiFilePickerAsync(string title)
    {
        var window = GetApplicationWindow();
        var provider = window.StorageProvider;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = [FilePickerFileTypes.All],
        });

        return files.ToList();
    }

    public static string GetTranslation(string key)
    {
        var assembly = typeof(RailworksForge.Translations.Strings).Assembly;
        const string sourceFile = $"{nameof(RailworksForge.Translations)}/{nameof(RailworksForge.Translations.Strings)}.toml";

        return TranslationProvider.ReadTranslation(assembly, sourceFile, key, TranslationProvider.Culture);
    }
}
