using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using Echoes;

namespace RailworksForge.Util;

public static class Utils
{
    public static Window GetApplicationWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window;
        }

        throw new Exception("could not get application window");
    }

    public static string GetTranslation(string key)
    {
        var assembly = typeof(Translations.Strings).Assembly;
        const string sourceFile = $"{nameof(Translations)}/{nameof(Translations.Strings)}.toml";

        return TranslationProvider.ReadTranslation(assembly, sourceFile, key, TranslationProvider.Culture);
    }
}
