using System.Diagnostics;

using CliWrap;

using RailworksForge.Core.Common;
using RailworksForge.Core.Config;
using RailworksForge.Core.Extensions;

using Serilog;

namespace RailworksForge.Core.Serialization;

public class Serz
{
    public record ConvertedSerzFile(string OutputPath);

    internal delegate Task NativeConverter(string inputPath, string outputPath, CancellationToken token);

    internal sealed record SerzConversion
    {
        public required string InputPath { get; init; }

        public required string OutputPath { get; init; }

        public bool UseInternalSerz { get; init; }

        public bool Force { get; init; }
    }

    public static Task<ConvertedSerzFile> Convert(string inputPath, CancellationToken token = default, bool force = false)
    {
        token.ThrowIfCancellationRequested();

        if (Paths.Exists(inputPath) is false)
        {
            throw new Exception($"serz tried to access file that does not exist: {inputPath}");
        }

        var conversion = new SerzConversion
        {
            InputPath = inputPath,
            OutputPath = Paths.GetAssetCachePath(inputPath, IsBinary(inputPath)),
            UseInternalSerz = Configuration.Get().UseInternalSerz,
            Force = force,
        };

        return Convert(conversion, RunSerz, token);
    }

    internal static async Task<ConvertedSerzFile> Convert(
        SerzConversion conversion,
        NativeConverter runNativeSerz,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var inputPath = conversion.InputPath;
        var outputPath = conversion.OutputPath;
        var isBin = IsBinary(inputPath);
        var inputWrittenAt = File.GetLastWriteTimeUtc(inputPath);

        if (conversion.Force is false && IsCacheCurrent(outputPath, inputWrittenAt, isBin))
        {
            return new ConvertedSerzFile(outputPath);
        }

        // Converted into a unique temporary file and moved into place, so a cancelled, failed or concurrent
        // conversion never leaves a partial file that a later call would trust as a cache hit.
        var outputDirectory = Path.GetDirectoryName(outputPath)!;
        var temporaryFilename = $"{Path.GetFileNameWithoutExtension(outputPath)}.{Guid.NewGuid():N}{Path.GetExtension(outputPath)}";
        var temporaryOutputPath = Path.Join(outputDirectory, temporaryFilename);

        var useInternalSerz = isBin && conversion.UseInternalSerz;

        var sw = Stopwatch.StartNew();

        try
        {
            if (useInternalSerz)
            {
                await Task.Run(() => SerzInternal.Convert(inputPath, temporaryOutputPath, token), token);
            }
            else
            {
                await runNativeSerz(inputPath, temporaryOutputPath, token);
            }

            if (!File.Exists(temporaryOutputPath))
            {
                throw new Exception("Serz execution failed");
            }

            if (isBin)
            {
                File.SetLastWriteTimeUtc(temporaryOutputPath, inputWrittenAt);
            }

            File.Move(temporaryOutputPath, outputPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryOutputPath);
        }

        var outputType = isBin ? "xml" : "bin";
        Log.Debug("converted file {Input} to {Format} in {Ms}ms", inputPath.ToWindowsPath().ToRelativeGamePath(), outputType, sw.ElapsedMilliseconds);

        sw.Stop();

        return new ConvertedSerzFile(outputPath);
    }

    // XML converted from a BIN carries that BIN's write time, so it only matches that exact BIN. A newer-than test
    // would accept it for an older BIN too, such as a restored backup, and an edit would then save the stale XML.
    private static bool IsCacheCurrent(string outputPath, DateTime inputWrittenAt, bool isBin)
    {
        if (!File.Exists(outputPath))
        {
            return false;
        }

        var outputWrittenAt = File.GetLastWriteTimeUtc(outputPath);

        return isBin ? outputWrittenAt == inputWrittenAt : outputWrittenAt >= inputWrittenAt;
    }

    private static bool IsBinary(string path)
    {
        return Path.GetExtension(path) == ".bin";
    }

    private static async Task RunSerz(string inputPath, string outputPath, CancellationToken token)
    {
        var exePath = Path.Join(Paths.GetGameDirectory(), "serz64.exe");
        var outputType = IsBinary(inputPath) ? "xml" : "bin";
        var inputArg = inputPath.ToWindowsPath();
        var outputArg = @$"\{outputType}: {outputPath.ToWindowsPath()}";

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
