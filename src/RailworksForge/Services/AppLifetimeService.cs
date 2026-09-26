using System;
using System.Diagnostics;
using System.IO;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace RailworksForge.Services;

public class AppLifetimeService
{
    public void Restart()
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
}
