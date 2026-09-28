using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using RailworksForge.Util;

using Serilog;

namespace RailworksForge.Services;

public class LauncherService
{
    public void OpenDirectory(string path)
    {
        _ = LaunchDirectory(path);
    }

    public void OpenOrCreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        OpenDirectory(path);
    }

    private static async Task LaunchDirectory(string path)
    {
        try
        {
            var launcher = Utils.GetApplicationWindow().Launcher;
            var launched = await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(path));

            if (!launched)
            {
                Log.Warning("Could not open directory {Path}", path);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to open directory {Path}", path);
        }
    }
}
