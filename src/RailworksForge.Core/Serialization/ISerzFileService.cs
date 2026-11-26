namespace RailworksForge.Core.Serialization;

public interface ISerzFileService
{
    Task<string> ConvertBinToXml(string path, CancellationToken cancellationToken);

    Task<string> ConvertXmlToBin(string path, CancellationToken cancellationToken);
}
