using System.Text;

using CliWrap;

using RailworksForge.Core.Proton;

namespace RailworksForge.Core;

public class SubProcess
{
    private static readonly ProtonService _protonService = new ();

    public record ExecOutput(string StdOut, string StdError);

    public static async Task<ExecOutput> ExecProcess(string path, List<string> arguments)
    {
        var proton = _protonService.GetProtonInstance();
        var environmentVariables = GetEnvironmentVariables();

        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var args = new List<string>{ path }.Concat(arguments).ToList();

        var workingDir = Path.GetDirectoryName(path);

        if (workingDir is null)
        {
            throw new Exception($"could not find working dir {workingDir}");
        }

        await Cli.Wrap(proton.WineBinPath)
            .WithEnvironmentVariables(environmentVariables)
            .WithArguments(args)
            .WithWorkingDirectory(workingDir)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        var output = stdOutBuffer.ToString();
        var error = stdErrBuffer.ToString();

        return new ExecOutput(output, error);
    }

    private static IReadOnlyDictionary<string, string?> GetEnvironmentVariables()
    {
        var proton = _protonService.GetProtonInstance();

        return new Dictionary<string, string?>
        {
            { "STEAM_COMPAT_DATA_PATH", proton.SteamCompatDataPath },
            { "STEAM_COMPAT_CLIENT_INSTALL_PATH", proton.SteamCompatClientInstallPath },
            { "WINEPREFIX", proton.PrefixPath },
            { "WINEFSYNC", "1" },
        };
    }
}
