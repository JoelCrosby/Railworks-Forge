using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

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
}
