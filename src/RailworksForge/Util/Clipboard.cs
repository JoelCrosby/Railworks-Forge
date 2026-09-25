using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

using RailworksForge.Views;

namespace RailworksForge.Util;

public class Clipboard
{
    public static IClipboard Get()
    {
        if (Utils.GetApplicationWindow() is MainWindow window)
        {
            return window.Clipboard ?? throw new Exception("unable to get clipboard instance from window");
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime app)
        {
            var topLevel = TopLevel.GetTopLevel(app.MainView);

            if (topLevel is not null)
            {
                return topLevel.Clipboard ?? throw new Exception("unable to get clipboard instance from top level view");
            }
        }

        throw new Exception("unable to get clipboard instance");
    }
}
