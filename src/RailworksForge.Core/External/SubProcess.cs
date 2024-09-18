using CliWrap;

using Polly;
using Polly.Retry;

using RailworksForge.Core.Proton;

using Serilog;

namespace RailworksForge.Core.External;

internal class SubProcess
{
    private static readonly ProtonInstance ProtonInstance = new ProtonService().GetProtonInstance();

    private static readonly RetryStrategyOptions OptionsOnRetry = new ()
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromMilliseconds(200),
        OnRetry = static args =>
        {
            Log.Warning("ProtonInstance ExecProcess OnRetry, Attempt: {Number}", args.AttemptNumber);
            return default;
        },
    };

    internal static async Task ExecProcess(string path, List<string> arguments)
    {
        var proton = ProtonInstance;
        var environmentVariables = GetEnvironmentVariables();

        var args = new [] { path }.Concat(arguments).ToArray();

        var workingDir = Path.GetDirectoryName(path);

        if (workingDir is null)
        {
            throw new Exception($"could not find working dir {workingDir}");
        }

        var pipeline = new ResiliencePipelineBuilder().AddRetry(OptionsOnRetry).Build();

        await pipeline.ExecuteAsync(async token => await Cli.Wrap(proton.WineBinPath)
            .WithEnvironmentVariables(environmentVariables)
            .WithArguments(args)
            .WithWorkingDirectory(workingDir)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(token)
        );
    }

    private static Dictionary<string, string?> GetEnvironmentVariables()
    {
        var proton = ProtonInstance;

        return new Dictionary<string, string?>
        {
            { "STEAM_COMPAT_DATA_PATH", proton.SteamCompatDataPath },
            { "STEAM_COMPAT_CLIENT_INSTALL_PATH", proton.SteamCompatClientInstallPath },
            { "WINEPREFIX", proton.PrefixPath },
            { "WINEFSYNC", "1" },
        };
    }
}
