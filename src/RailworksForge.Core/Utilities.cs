using AngleSharp.Dom;

using RailworksForge.Core.Extensions;

using Serilog;

namespace RailworksForge.Core;

public static class Utilities
{
    public const string NS = "http://www.kuju.com/TnT/2003/Delta";

    public static IElement GenerateEntityContainerItem(this IDocument document)
    {
        var node = document.CreateXmlElement("e");
        node.SetAttribute(NS, "d:numElements", "16");
        node.SetAttribute(NS, "d:elementType", "sFloat32");
        node.SetAttribute(NS, "d:precision", "string");
        node.SetTextContent("1.0000000 0.0000000 0.0000000 0.0000000 0.0000000 1.0000000 0.0000000 0.0000000 0.0000000 0.0000000 1.0000000 0.0000000 0.0000000 0.0000000 0.0000000 1.0000000");
        return node;
    }

    public static IElement GenerateCargoComponentItem(this IDocument document, string val, string altEncoding)
    {
        var node = document.CreateXmlElement("e");
        node.SetAttribute(NS, "d:type", "sFloat32");
        node.SetAttribute(NS, "d:alt_encoding", altEncoding);
        node.SetAttribute(NS, "d:precision", "string");
        node.SetTextContent(val);
        return node;
    }

    private static Tuple<ulong, ulong> GetUuidLongs(Guid guid)
    {
        var result = new Tuple<ulong, ulong>(
            BitConverter.ToUInt64(guid.ToByteArray(), 0),
            BitConverter.ToUInt64(guid.ToByteArray(), 8));

        return result;
    }

    public static IElement GenerateCGuid(this IDocument document)
    {
        var guid = Guid.NewGuid();
        var ulongs = GetUuidLongs(guid);
        var cGuid = document.CreateXmlElement("cGUID");
        var uuid = document.CreateXmlElement("UUID");

        var e1 = document.CreateXmlElement("e");
        e1.SetAttribute(NS, "d:type", "sUInt64");
        e1.SetTextContent(ulongs.Item1.ToString());

        var e2 = document.CreateXmlElement("e");
        e2.SetAttribute(NS, "d:type", "sUInt64");
        e2.SetTextContent(ulongs.Item2.ToString());

        uuid.AppendNodes(e1, e2);

        var devString = document.CreateXmlElement("DevString");
        devString.SetAttribute(NS, "d:type", "cDeltaString");
        devString.SetTextContent(guid.ToString());

        cGuid.AppendNodes(uuid, devString);
        return cGuid;
    }

    public static string GetBackupArchiveName()
    {
        return $"backup-{Guid.NewGuid().ToString()[..6]}-{DateTimeOffset.UtcNow:dd-MMM-yy_hh-mm}.zip";
    }

    public static BlueprintType ParseBlueprintType(string? tagName)
    {
        var result = tagName switch
        {
            "cEngine" => BlueprintType.Engine,
            "cEngineBlueprint" => BlueprintType.Engine,
            "cWagon" => BlueprintType.Wagon,
            "cWagonBlueprint" => BlueprintType.Wagon,
            "cTender" => BlueprintType.Tender,
            "cTenderBlueprint" => BlueprintType.Tender,
            "cNamedTextureSetBlueprint" => BlueprintType.TextureSet,
            "cReskinBlueprint" => BlueprintType.Reskin,
            "cSpotLightBlueprint" => BlueprintType.Spotlight,
            "cBogeyBlueprint" => BlueprintType.Bogey,
            "cSceneryBlueprint" => BlueprintType.Scenery,
            "cAnimSceneryBlueprint" => BlueprintType.AnimatedScenery,
            "cEngineSimBlueprint" => BlueprintType.EngineSim,
            "cCouplingTypeBlueprint" => BlueprintType.CouplingType,
            "cHeadOutCameraBlueprint" => BlueprintType.HeadOutCamera,
            "cCabCameraBlueprint" => BlueprintType.CabCamera,
            "cPointLightBlueprint" => BlueprintType.PointLight,
            "cEmitterBlueprint" => BlueprintType.Emitter,
            "cHeadLightBlueprint" => BlueprintType.HeadLight,
            "cProceduralShapeBlueprint" => BlueprintType.ProceduralShape,
            "cSoundBlueprint" => BlueprintType.Sound,
            "cCabOcclusionBlueprint" => BlueprintType.CabOcclusion,
            "cAnimProceduralSceneryBlueprint" => BlueprintType.AnimProceduralScenery,
            "cInputMapperBlueprint" => BlueprintType.InputMapper,
            "cDriverBlueprint" => BlueprintType.Driver,
            "cEngineSimSubSystemBlueprint" => BlueprintType.EngineSimSubSystem,
            "cFiremanBlueprint" => BlueprintType.Fireman,
            "cEditorShapeBlueprint" => BlueprintType.EditorShape,
            "cAnalogClockBlueprint" => BlueprintType.AnalogClock,
            "cDigitalClockBlueprint" => BlueprintType.DigitalClock,
            "cScriptableSceneryBlueprint" => BlueprintType.ScriptableSceneryBlueprint,
            "cConsistBlueprint" => BlueprintType.ConsistBlueprint,
            "cMetadataBlueprint" => BlueprintType.MetadataBlueprint,
            "cConsistFragmentBlueprint" => BlueprintType.ConsistFragmentBlueprint,
            "cTerrainTextureBluePrint" => BlueprintType.TerrainTextureBluePrint,
            "cSignalBlueprint" => BlueprintType.SignalBlueprint,
            _ => BlueprintType.Unknown,
        };

        if (result == BlueprintType.Unknown)
        {
            Log.Warning("unknown blueprint type for tag with the following name '{TagName}'", tagName);
        }

        return result;
    }

    private static readonly char Sp = Path.DirectorySeparatorChar;

    public static readonly string[] RollingStockFolders =
    [
        $"{Sp}Electric{Sp}",
        $"{Sp}Diesel{Sp}",
        $"{Sp}Steam{Sp}",
        $"{Sp}Wagon{Sp}",
        $"{Sp}Tender{Sp}",
    ];
}
