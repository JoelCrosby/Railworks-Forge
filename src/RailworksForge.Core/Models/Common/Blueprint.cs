namespace RailworksForge.Core.Models.Common;

public abstract class Blueprint
{
    public required string BlueprintId { get; init; }

    public required string BlueprintSetIdProvider { get; init; }

    public required string BlueprintSetIdProduct { get; init; }

    public AcquisitionState AcquisitionState => GetAcquisitionState();

    private string ProductDirectory => Path.Join(
        Paths.GetAssetsDirectory(),
        BlueprintSetIdProvider,
        BlueprintSetIdProduct
    );

    private AcquisitionState GetAcquisitionState()
    {
        var agnosticBlueprintIdPath = BlueprintId.Replace('\\', Path.DirectorySeparatorChar);
        var binaryPath = agnosticBlueprintIdPath.Replace(".xml", ".bin");
        var blueprintPath = Path.Join(ProductDirectory, binaryPath);

        if (File.Exists(blueprintPath))
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
            var found = Archives.EntryExists(archive, binaryPath);

            if (found)
            {
                return AcquisitionState.Found;
            }
        }

        return AcquisitionState.Missing;
    }
}
