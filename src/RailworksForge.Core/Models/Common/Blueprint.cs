using RailworksForge.Core.External;

namespace RailworksForge.Core.Models.Common;

public abstract class Blueprint
{
    public required string BlueprintId { get; init; }

    public required string BlueprintSetIdProvider { get; init; }

    public required string BlueprintSetIdProduct { get; init; }

    public AcquisitionState AcquisitionState => GetAcquisitionState();

    public async Task<string> GetBlueprintXml()
    {
        if (File.Exists(BlueprintPath))
        {
            var converted = await Serz.Convert(BlueprintPath);
            return await File.ReadAllTextAsync(converted.OutputPath);
        }


        var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            var extracted = Archives.ExtractFileContentFromPath(archive, BinaryPath, BlueprintPath);

            if (extracted)
            {
                var result = await Serz.Convert(BlueprintPath);
                return await File.ReadAllTextAsync(result.OutputPath);
            }
        }

        throw new Exception($"unable to get blueprint xml for path {BlueprintPath}");
    }

    private AcquisitionState _cachedAcquisitionState;

    private string ProductDirectory => Path.Join(
        Paths.GetAssetsDirectory(),
        BlueprintSetIdProvider,
        BlueprintSetIdProduct
    );

    private string AgnosticBlueprintIdPath => BlueprintId.Replace('\\', Path.DirectorySeparatorChar);
    private string BinaryPath => AgnosticBlueprintIdPath.Replace(".xml", ".bin");
    private string BlueprintPath => Path.Join(ProductDirectory, BinaryPath);

    private AcquisitionState GetAcquisitionState()
    {
        if (_cachedAcquisitionState is not AcquisitionState.Unknown)
        {
            return _cachedAcquisitionState;
        }

        _cachedAcquisitionState = LoadAcquisitionState();
        return _cachedAcquisitionState;
    }

    private AcquisitionState LoadAcquisitionState()
    {
        if (Paths.Exists(BlueprintPath, Paths.GetAssetsDirectory()))
        {
            return AcquisitionState.Found;
        }

        if (!Directory.Exists(ProductDirectory))
        {
            return AcquisitionState.Missing;
        }

        var archives = Directory.EnumerateFiles(ProductDirectory, "*.ap", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            var found = Archives.EntryExists(archive, BinaryPath);

            if (found)
            {
                return AcquisitionState.Found;
            }
        }

        return AcquisitionState.Missing;
    }
}
