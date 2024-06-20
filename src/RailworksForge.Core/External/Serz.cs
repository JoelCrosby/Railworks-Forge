using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace RailworksForge.Core.External;

public class Serz
{
    public record ConvertedSerzFile(string InputPath, string OutputPath)
    {
        public IHtmlDocument Parse()
        {
            var content = File.ReadAllText(OutputPath);
            return new HtmlParser().ParseDocument(content);
        }
    }

    public static async Task<ConvertedSerzFile> Convert(string inputPath, CancellationToken cancellationToken = default, bool force = false)
    {
        var isBin = Path.GetExtension(inputPath) == ".bin";
        var outputPath = isBin ? inputPath.Replace(".bin", ".bin.xml") : inputPath.Replace(".bin.xml", ".bin");

        if (force is false && File.Exists(outputPath))
        {
            return new ConvertedSerzFile(inputPath, outputPath);
        }

        var exe = Path.Join(Paths.GetGameDirectory(), "serz64.exe");
        var inputArg = inputPath.ToWindowsPath();
        var outputType = isBin ? "xml" : "bin";
        var outputArg = @$"\{outputType}: {outputPath.ToWindowsPath()}";

        var commandOutput = await SubProcess.ExecProcess(exe, [inputArg, outputArg], cancellationToken);
        var isSuccess = File.Exists(outputPath);

        if (!isSuccess)
        {
            throw new Exception($"serz execution failed: {commandOutput.StdError}");
        }

        return new ConvertedSerzFile(inputPath, outputPath);
    }
}
