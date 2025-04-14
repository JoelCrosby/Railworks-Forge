using System.Diagnostics;
using Avalonia.Platform;
using RailworksForge.Core;

namespace RailworksForge.Util;

public static class Launcher
{
    public static void Open(string path)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = Paths.GetPlatform() is Paths.Platform.Windows ? "explorer" : "xdg-open",
            Arguments = $"\"{path}\"",
        };

        Process.Start(processInfo);
    }
}
