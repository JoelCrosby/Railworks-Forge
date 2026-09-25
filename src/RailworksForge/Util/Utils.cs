using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    public static async Task<IStorageFolder?> OpenFolderPickerAsync(string title)
    {
        var window = GetApplicationWindow();
        var provider = window.StorageProvider;

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.Count >= 1 ? folders[0] : null;
    }

    public static void RestartApplication()
    {

        if (Environment.ProcessPath is not {} processPath)
        {
            return;
        }

        var startInfo = new ProcessStartInfo(processPath) { UseShellExecute = false };
        var isDotnetHost = Path.GetFileNameWithoutExtension(processPath) == "dotnet";

        if (isDotnetHost)
        {
            startInfo.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        }

        Process.Start(startInfo);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public static string GetTranslation(string key)
    {
        var assembly = typeof(Translations.Strings).Assembly;
        const string sourceFile = $"{nameof(Translations)}/{nameof(Translations.Strings)}.toml";

        return TranslationProvider.ReadTranslation(assembly, sourceFile, key, TranslationProvider.Culture);
    }
}
