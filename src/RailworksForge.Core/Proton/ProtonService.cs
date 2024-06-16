using System.Diagnostics;

using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;

using VdfParser;


namespace RailworksForge.Core.Proton;

public class ProtonService
{
    private static ProtonInstance? _instance;

    private const string TargetAppId = "24010";

    private readonly List<string> _commonSteamDirs =
    [
        ".steam/steam",
        ".local/share/Steam",
    ];

    public ProtonInstance GetProtonInstance()
    {
        if (_instance is not null)
        {
            return _instance;
        }

        var steamInstallation = FindSteamInstallations().FirstOrDefault();

        if (steamInstallation is null)
        {
            throw new Exception("could not find steam installation");
        }

        var steamLibraryPaths = FindSteamLibraryPaths(steamInstallation.Path);
        var steamApps = FindSteamApps(steamInstallation.Path, steamLibraryPaths);

        var railworksApp = steamApps.FirstOrDefault(app => app.AppId == TargetAppId);

        if (railworksApp is null)
        {
            throw new Exception("could not find railworks steam app");
        }

        var protonApp = FindProtonApp(steamInstallation.Path, steamApps, TargetAppId);

        var steamCompatDataPath = Path.Join(railworksApp.LibraryPath, "steamapps", "compatdata", TargetAppId);
        var steamCompatClientInstallPath = steamInstallation.Path;
        var executable = Path.Join(protonApp.InstallDir, "proton");
        var directory = protonApp.InstallDir;

        _instance = new ProtonInstance
        (
            steamCompatDataPath,
            steamCompatClientInstallPath,
            executable,
            directory
        );

        return _instance;
    }

    private record SteamInstallation(string Path);

    private List<SteamInstallation> FindSteamInstallations()
    {
        var installations = new List<SteamInstallation>();

        var home = Paths.GetHomeDirectory();
        var steamRoot = Path.Join(Paths.GetHomeDirectory(), ".steam", "root", "ubuntu12_32");

        if (!Directory.Exists(steamRoot))
        {
            return [];
        }

        foreach (var steamPath in _commonSteamDirs)
        {
            var path = Path.Join(home, steamPath);

            if (HasSteamAppsDir(path))
            {
                installations.Add(new SteamInstallation(path));
            }
        }

        return installations;
    }

    public record SteamApp
    {
        public required string InstallDir { get; init; }
        public required string Name { get; init; }
        public required string AppId { get; init; }
        public string? LibraryPath { get; init; }

        public virtual bool Equals(SteamApp? other)
        {
            return other is not null && AppId.Equals(other.AppId);
        }

        public override int GetHashCode()
        {
            return AppId.GetHashCode();
        }
    }

    private List<SteamApp> FindSteamApps(string steamRoot, List<string> steamLibraryPaths)
    {
        var apps = new HashSet<SteamApp>();

        foreach (var steamLibraryPath in steamLibraryPaths)
        {
            if (!Directory.Exists(steamLibraryPath))
            {
                continue;
            }

            var steamappsDir = GetSteamappsSubdirectory(steamLibraryPath);

            if (steamappsDir is null) continue;

            var appManifestPaths = Directory.EnumerateFiles(steamappsDir, "appmanifest_*.acf", SearchOption.TopDirectoryOnly);

            foreach (var appManifestPath in appManifestPaths)
            {
                try
                {
                    var content = File.ReadAllText(appManifestPath);
                    var manifestVdf = VdfConvert.Deserialize(content).ToJson();
                    var root = manifestVdf.Value;

                    var appId = root.Value<string>("appid");
                    var name = root.Value<string>("name");
                    var installDir = root.Value<string>("installdir");

                    apps.Add(new SteamApp
                    {
                        AppId = appId,
                        Name = name,
                        InstallDir = installDir,
                        LibraryPath = steamLibraryPath,
                    });
                }
                catch
                {
                    Debug.WriteLine($"failed to read app manifest at {appManifestPath}");
                }
            }
        }

        foreach (var app in GetCustomCompatToolInstallations(steamRoot))
        {
            apps.Add(app);
        }

        return apps.OrderBy(app => app.Name).ToList();
    }

    private string? GetSteamappsSubdirectory(string path)
    {
        return GetSubDir(path, "steamapps");
    }

    private List<string> FindSteamLibraryPaths(string steamPath)
    {
        var steamAppsDir = GetSteamappsSubdirectory(steamPath);
        var foldersVdfPath = Path.Join(steamAppsDir, "libraryfolders.vdf");

        return ParseLibraryFolders(foldersVdfPath);
    }

    private List<string> ParseLibraryFolders(string path)
    {
        var content = File.ReadAllText(path);
        var libraryVdf = VdfConvert.Deserialize(content).ToJson();

        return libraryVdf.Value.Select(child => child.Children().First().Value<string>("path")).ToList();
    }

    private List<SteamApp> GetCustomCompatToolInstallations(string steamRoot)
    {
        var customToolApps = new List<SteamApp>();

        var compatToolDirs = GetCompatToolDirs(steamRoot);

        foreach (var compatToolDir in compatToolDirs)
        {
            var apps = GetCustomCompatToolInstallationsInDir(compatToolDir);

            customToolApps.AddRange(apps);
        }

        return customToolApps;
    }

    private List<string> GetCompatToolDirs(string steamRoot)
    {
        var paths = new List<string>
        {
            "/usr/share/steam/compatibilitytools.d",
            "/usr/local/share/steam/compatibilitytools.d"
        };

        if (Environment.GetEnvironmentVariable("STEAM_EXTRA_COMPAT_TOOLS_PATHS") is { } extra)
        {
            paths.Add(extra);
        }

        paths.Add(Path.Join(steamRoot, "compatibilitytools.d"));

        return paths;
    }

    private static List<SteamApp> GetCustomCompatToolInstallationsInDir(string path)
    {
        var apps = new List<SteamApp>();

        if (!Directory.Exists(path))
        {
            return apps;
        }

        var compToolFiles = Directory.EnumerateFiles(path, "compatibilitytool.vdf", SearchOption.AllDirectories);

        foreach (var vdfPath in compToolFiles)
        {
            var data = new VdfDeserializer().Deserialize(File.OpenRead(vdfPath));
            var compatTools = (IDictionary<string, object>) data.compatibilitytools.compat_tools;

            var app = new SteamApp
            {
                Name = compatTools.Keys.ElementAt(0),
                AppId = Guid.NewGuid().ToString(),
                InstallDir = Directory.GetParent(vdfPath)!.FullName,
            };

            apps.Add(app);
        }

        return apps;
    }

    private bool HasSteamAppsDir(string path)
    {
        return GetSubDir(path, "steamapps") is not null;
    }

    private string? GetSubDir(string path, string subdirectory)
    {
        return Directory
            .EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(dir => string.Equals(Path.GetFileName(dir), subdirectory, StringComparison.OrdinalIgnoreCase));
    }

    private SteamApp FindProtonApp(string steamPath, List<SteamApp> steamApps, string appId)
    {
        if (Environment.GetEnvironmentVariable("PROTON_VERSION") is { } protonVersion)
        {
            var protonApp = steamApps.FirstOrDefault(app => app.Name == protonVersion);

            if (protonApp is not null) return protonApp;

            throw new Exception($"could, not find proton app for PROTON_VERSION {protonVersion}");
        }

        var configVdfPath = Path.Join(steamPath, "config", "config.vdf");

        var result = new VdfDeserializer().Deserialize(File.OpenRead(configVdfPath));
        var toolMapping = result.InstallConfigStore.Software.Valve.Steam.CompatToolMapping;
        var appMapping = (dynamic)((IDictionary<string, object>)toolMapping)[appId];

        var appName = appMapping.name as string;

        return steamApps.FirstOrDefault(app => app.Name == appName) ?? throw new Exception("could not find proton app");
    }
}
