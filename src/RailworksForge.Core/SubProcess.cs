using System.Text;

using CliWrap;

namespace RailworksForge.Core;

public class SubProcess
{
    public record ExecOutput(string StdOut, string StdError);

    private static readonly IReadOnlyDictionary<string, string?> EnvironmentVariables = new Dictionary<string, string?>
    {
        { "STEAM_COMPAT_DATA_PATH", Proton.Instance.SteamCompatDataPath },
        { "STEAM_COMPAT_CLIENT_INSTALL_PATH", Proton.Instance.SteamCompatClientInstallPath },
        { "WINEPREFIX", Proton.Instance.PrefixPath },
        { "WINEFSYNC", "1" },
    };

    public static async Task<ExecOutput> ExecProcess(string path, List<string> arguments)
    {
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var args = new List<string>{ path }.Concat(arguments).ToList();

        var workingDir = Path.GetDirectoryName(path);

        if (workingDir is null)
        {
            throw new Exception($"could not find working dir {workingDir}");
        }

        await Cli.Wrap(Proton.Instance.WineBinPath)
            .WithEnvironmentVariables(EnvironmentVariables)
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
}
