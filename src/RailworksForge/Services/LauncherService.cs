using System.IO;

using Avalonia.Platform.Storage;

using RailworksForge.Util;

namespace RailworksForge.Services;

public class LauncherService
{
    public void OpenDirectory(string path)
    {
        var launcher = Utils.GetApplicationWindow().Launcher;
        var directory = new DirectoryInfo(path);

        launcher.LaunchDirectoryInfoAsync(directory);
    }

    public void OpenOrCreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        OpenDirectory(path);
    }
}
