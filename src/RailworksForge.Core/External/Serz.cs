using CliWrap;

namespace RailworksForge.Core.External;

public class Serz
{
    public record ConvertedSerzFile(string OutputPath);

    private static readonly string ExePath = Path.Join(Paths.GetGameDirectory(), "serz64.exe");

    public static async Task<ConvertedSerzFile> Convert(string inputPath, CancellationToken token = default, bool force = false)
    {
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

        await RunSerz(inputArg, outputArg, token);

        var isSuccess = Paths.Exists(outputPath);

        if (!isSuccess)
        {
            throw new Exception("Serz execution failed");
        }

        return new ConvertedSerzFile(outputPath);
    }

    private static async Task RunSerz(string inputArg, string outputArg, CancellationToken token)
    {
        if (Paths.GetPlatform() == Paths.Platform.Windows)
        {
            await Cli.Wrap(ExePath).WithArguments([inputArg, outputArg]).ExecuteAsync(token);
        }
        else
        {
            await Cli.Wrap("wine").WithArguments([ExePath, inputArg, outputArg]).ExecuteAsync(token);
        }
    }
}
