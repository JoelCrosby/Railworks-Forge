// using System;
//

using System.Diagnostics;

namespace RailworksForge.Util;

public static class Launcher
{
    public static void Open(string path)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "xdg-open",
            Arguments = path,
        };

        Process.Start(processInfo);
    }
}
