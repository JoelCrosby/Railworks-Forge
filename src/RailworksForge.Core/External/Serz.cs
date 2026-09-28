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

        var hasCachedOutput = File.Exists(outputPath);
        var isCacheCurrent = hasCachedOutput && File.GetLastWriteTimeUtc(outputPath) >= File.GetLastWriteTimeUtc(inputPath);

        if (force is false && isCacheCurrent)
        {
            return new ConvertedSerzFile(outputPath);
        }

        // Converted into a unique temporary file and moved into place, so a cancelled, failed or concurrent
        // conversion never leaves a partial file that a later call would trust as a cache hit.
        var outputDirectory = Path.GetDirectoryName(outputPath)!;
        var temporaryFilename = $"{Path.GetFileNameWithoutExtension(outputPath)}.{Guid.NewGuid():N}{Path.GetExtension(outputPath)}";
        var temporaryOutputPath = Path.Join(outputDirectory, temporaryFilename);

        var inputArg = inputPath.ToWindowsPath();
        var outputType = isBin ? "xml" : "bin";
        var outputArg = @$"\{outputType}: {temporaryOutputPath.ToWindowsPath()}";

        var sw = Stopwatch.StartNew();

        var useInternalSerz = isBin && Configuration.Get().UseInternalSerz;

        try
        {
            if (useInternalSerz)
            {
                await Task.Run(() => SerzInternal.Convert(inputPath, temporaryOutputPath, token), token);
            }
            else
            {
                await RunSerz(inputArg, outputArg, token);
            }

            if (!File.Exists(temporaryOutputPath))
            {
                throw new Exception("Serz execution failed");
            }

            File.Move(temporaryOutputPath, outputPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryOutputPath);
        }

        Log.Debug("converted file {Input} to {Format} in {Ms}ms", inputArg.ToRelativeGamePath(), outputType, sw.ElapsedMilliseconds);

        sw.Stop();

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
