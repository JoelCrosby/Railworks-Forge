using RailworksForge.Core.Common;

namespace RailworksForge.Core.Config;

public enum GameDirectoryChange
{
    Invalid,
    Saved,
    RestartRequired,
}

// The directory is resolved once at startup and read by services for the rest of the session,
// so saving a different one only takes effect after a restart.
public class GameDirectoryService(ISettingsService settings) : IGameDirectoryService
{
    public string Current => Paths.GetGameDirectory();

    public bool IsCurrentValid => IsValid(Current);

    public bool IsValid(string? path)
    {
        return Paths.IsValidGameDirectory(path);
    }

    public void Resolve()
    {
        Paths.SetGameDirectory();
    }

    public GameDirectoryChange Change(string path)
    {
        if (!IsValid(path))
        {
            return GameDirectoryChange.Invalid;
        }

        settings.Update(config => config with { GameDirectoryPath = path });

        var hasCurrent = !string.IsNullOrWhiteSpace(Current);
        var isCurrent = hasCurrent && IsSamePath(path, Current);

        return isCurrent ? GameDirectoryChange.Saved : GameDirectoryChange.RestartRequired;
    }

    private static bool IsSamePath(string first, string second)
    {
        var normalisedFirst = Path.TrimEndingDirectorySeparator(Path.GetFullPath(first));
        var normalisedSecond = Path.TrimEndingDirectorySeparator(Path.GetFullPath(second));

        return normalisedFirst == normalisedSecond;
    }
}
