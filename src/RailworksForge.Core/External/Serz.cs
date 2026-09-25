using System.Diagnostics;

using CliWrap;

using RailworksForge.Core.Config;
using RailworksForge.Core.Extensions;

using Serilog;

namespace RailworksForge.Core.External;

public class Serz
{
    public record ConvertedSerzFile(string OutputPath);

    public static async Task<ConvertedSerzFile> Convert(string inputPath, CancellationToken token = default, bool force = false)
    {
        token.ThrowIfCancellationRequested();

        if (Paths.Exists(inputPath) is false)
        {
            throw new Exception($"serz tried to access file that does not exist: {inputPath}");
        }

        var isBin = Path.GetExtension(inputPath) == ".bin";
        var outputPath = Paths.GetAssetCachePath(inputPath, isBin);

        if (force is false && File.Exists(outputPath))
        {
            return new ConvertedSerzFile(outputPath);
        }

        var inputArg = inputPath.ToWindowsPath();
        var outputType = isBin ? "xml" : "bin";
        var outputArg = @$"\{outputType}: {outputPath.ToWindowsPath()}";

        var sw = Stopwatch.StartNew();

        var useInternalSerz = isBin && Configuration.Get().UseInternalSerz;

        if (useInternalSerz)
        {
            await Task.Run(() => SerzInternal.Convert(inputPath, outputPath, token), token);
        }
        else
        {
            await RunSerz(inputArg, outputArg, token);
        }

        Log.Debug("converted file {Input} to {Format} in {Ms}ms", inputArg.ToRelativeGamePath(), outputType, sw.ElapsedMilliseconds);

        sw.Stop();

        var isSuccess = Paths.Exists(outputPath);

        if (!isSuccess)
        {
            throw new Exception("Serz execution failed");
        }

        return new ConvertedSerzFile(outputPath);
    }

    private static async Task RunSerz(string inputArg, string outputArg, CancellationToken token)
    {
        var exePath = Path.Join(Paths.GetGameDirectory(), "serz64.exe");

        if (Paths.GetPlatform() == Paths.Platform.Windows)
        {
            await Cli.Wrap(exePath).WithArguments([inputArg, outputArg]).ExecuteAsync(token);
        }
        else
        {
            await Cli.Wrap("wine").WithArguments([exePath, inputArg, outputArg]).ExecuteAsync(token);
        }
    }
}
