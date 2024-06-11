using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

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
}
