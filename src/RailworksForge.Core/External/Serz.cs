namespace RailworksForge.Core.External;

public class Serz
{
    public record ConvertedSerzFile(string OutputPath);

    private static readonly string ExePath = Path.Join(Paths.GetGameDirectory(), "serz64.exe");

    public static async Task<ConvertedSerzFile> Convert(string inputPath, bool force = false)
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

        await SubProcess.ExecProcess(ExePath, [inputArg, outputArg]);

        var isSuccess = Paths.Exists(outputPath);

        if (!isSuccess)
        {
            throw new Exception("Serz execution failed");
        }

        return new ConvertedSerzFile(outputPath);
    }
}
