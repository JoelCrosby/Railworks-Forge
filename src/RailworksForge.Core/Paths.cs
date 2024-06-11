namespace RailworksForge.Core;

public static class Paths
{
    public static string GetHomeDirectory()
    {
        return Environment.GetEnvironmentVariable("HOME") ?? throw new Exception("Could not find user home directory");
    }

    public static string GetGameDirectory()
    {
        return "/cache/SteamLibrary/steamapps/common/RailWorks/";
    }

    public static string GetAssetsDirectory()
    {
        return Path.Join(GetGameDirectory(), "Assets");
    }

    public static string GetScenariosDirectory()
    {
        return Path.Join(GetGameDirectory(), "Content", "Routes");
    }

    public static string ToWindowsPath(this string path)
    {
        return path.Replace("/", @"\").Replace(@"\cache\SteamLibrary\steamapps\", @"s:\");
    }
}
