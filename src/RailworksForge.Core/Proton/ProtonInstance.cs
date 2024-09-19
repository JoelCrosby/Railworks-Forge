namespace RailworksForge.Core.Proton;

public class ProtonInstance
{
    public string SteamCompatDataPath { get; }

    public string SteamCompatClientInstallPath { get; }

    public string ExecutablePath { get; }

    public string ProtonDirectory { get; }

    public string PrefixPath { get; }

    public string WineBinPath { get; }

    public string WineServerPath { get; }

    public string SteamAppsPath { get; }

    public ProtonInstance(string steamCompatDataPath, string steamCompatClientInstallPath, string executablePath, string protonDirectory)
    {
        SteamCompatDataPath = steamCompatDataPath;
        SteamCompatClientInstallPath = steamCompatClientInstallPath;
        ExecutablePath = executablePath;
        ProtonDirectory = protonDirectory;

        PrefixPath = Path.Join(SteamCompatDataPath, "pfx");
        WineBinPath = Path.Join(ProtonDirectory, "files", "bin", "wine64");
        WineServerPath = Path.Join(ProtonDirectory, "files", "bin", "wineserver");

        SteamAppsPath = Directory.GetParent(SteamCompatDataPath)!.Parent!.FullName;

        var linkPath = Path.Join(PrefixPath, "dosdevices", "s:");

        if (!Directory.Exists(linkPath))
        {
            File.CreateSymbolicLink(linkPath, SteamAppsPath);
        }
    }
}
