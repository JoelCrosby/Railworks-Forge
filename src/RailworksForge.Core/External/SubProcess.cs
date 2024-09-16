using CliWrap;

using RailworksForge.Core.Proton;

namespace RailworksForge.Core.External;

internal class SubProcess
{
    private static readonly ProtonService ProtonService = new ();

    internal static async Task ExecProcess(string path, List<string> arguments, CancellationToken cancellationToken = default)
    {
        var proton = ProtonService.GetProtonInstance();
        var environmentVariables = GetEnvironmentVariables();

        var args = new [] { path }.Concat(arguments).ToArray();

        var workingDir = Path.GetDirectoryName(path);

        if (workingDir is null)
        {
            throw new Exception($"could not find working dir {workingDir}");
        }

        await Cli.Wrap(proton.WineBinPath)
            .WithEnvironmentVariables(environmentVariables)
            .WithArguments(args)
            .WithWorkingDirectory(workingDir)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(cancellationToken);
    }

    private static Dictionary<string, string?> GetEnvironmentVariables()
    {
        var proton = ProtonService.GetProtonInstance();

        return new Dictionary<string, string?>
        {
            { "STEAM_COMPAT_DATA_PATH", proton.SteamCompatDataPath },
            { "STEAM_COMPAT_CLIENT_INSTALL_PATH", proton.SteamCompatClientInstallPath },
            { "WINEPREFIX", proton.PrefixPath },
            { "WINEFSYNC", "1" },
        };
    }
}
