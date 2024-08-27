using AngleSharp.Dom;

using RailworksForge.Core.Extensions;

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

    public static VehicleType ParseVehicleType(string? tagName)
    {
        return tagName?.ToLowerInvariant() switch
        {
            "cengine" => VehicleType.Engine,
            "cengineblueprint" => VehicleType.Engine,
            "cwagon" => VehicleType.Wagon,
            "cwagonblueprint" => VehicleType.Wagon,
            "ctender" => VehicleType.Tender,
            "ctenderblueprint" => VehicleType.Tender,
            _ => throw new Exception($"unknown vehicle type for tag name {tagName}"),
        };
    }
}
