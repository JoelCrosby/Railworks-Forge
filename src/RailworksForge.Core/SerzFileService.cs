using RailworksForge.Core.External;

namespace RailworksForge.Core;

public class SerzFileService
{
    public async Task<string> ConvertBinToXml(string path, CancellationToken cancellationToken)
    {
        var result = await Serz.Convert(path, cancellationToken, true);
        var destination = $"{path}.xml";

        File.Copy(result.OutputPath, destination);

        return destination;
    }

    // Converting back is the second half of an edit round trip, so the original .bin is meant to be replaced.
    public async Task<string> ConvertXmlToBin(string path, CancellationToken cancellationToken)
    {
        var result = await Serz.Convert(path, cancellationToken, true);
        var destination = GetBinaryDestination(path);

        File.Copy(result.OutputPath, destination, true);

        return destination;
    }

    private static string GetBinaryDestination(string xmlPath)
    {
        var withoutXmlExtension = Path.ChangeExtension(xmlPath, null);
        var isExportedBinary = Path.GetExtension(withoutXmlExtension).Equals(".bin", StringComparison.OrdinalIgnoreCase);

        return isExportedBinary ? withoutXmlExtension : Path.ChangeExtension(xmlPath, ".bin");
    }
}
