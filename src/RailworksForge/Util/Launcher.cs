using System.IO;

using Avalonia.Platform.Storage;


namespace RailworksForge.Util;

public static class Launcher
{
    public static void Open(string path)
    {
        var launcher = Utils.GetApplicationWindow().Launcher;
        var info = new DirectoryInfo(path);

        launcher.LaunchDirectoryInfoAsync(info);
    }
}
