using AngleSharp.Dom;

namespace RailworksForge.Core.External;

public class Serz
{
    public record ConvertedSerzFile(string OutputPath)
    {
        public IDocument Parse()
        {
            var content = File.ReadAllText(OutputPath);
            return XmlParser.ParseDocument(content);
        }
    }

    public static async Task<ConvertedSerzFile> Convert(
        string inputPath,
        bool force = false,
        CancellationToken cancellationToken = default)
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

        var exe = Path.Join(Paths.GetGameDirectory(), "serz64.exe");
        var inputArg = inputPath.ToWindowsPath();
        var outputType = isBin ? "xml" : "bin";
        var outputArg = @$"\{outputType}: {outputPath.ToWindowsPath()}";

        var commandOutput = await SubProcess.ExecProcess(exe, [inputArg, outputArg], cancellationToken);
        var isSuccess = Paths.Exists(outputPath);

        if (!isSuccess)
        {
            throw new Exception($"serz execution failed: {commandOutput.StdError}");
        }

        return new ConvertedSerzFile(outputPath);
    }
}
