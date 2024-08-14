using AngleSharp.Dom;
using AngleSharp.Xml.Dom;

using RailworksForge.Core.Extensions;

namespace RailworksForge.Core;

public static class Utilities
{
    public const string DocumentNamespace = "http://www.kuju.com/TnT/2003/Delta";

    public static IElement GenerateEntityContainerItem(this IXmlDocument document)
    {
        var node = document.CreateElement("e");
        node.SetAttribute("numElements", "16");
        node.SetAttribute("elementType", "sFloat32");
        node.SetAttribute("precision", "string");
        node.SetTextContent("1.0000000 0.0000000 0.0000000 0.0000000 0.0000000 1.0000000 0.0000000 0.0000000 0.0000000 0.0000000 1.0000000 0.0000000 0.0000000 0.0000000 0.0000000 1.0000000");
        return node;
    }

    public static IElement GenerateCargoComponentItem(this IXmlDocument document, string val, string altEncoding)
    {
        var node = document.CreateElement("e");
        node.SetAttribute("type", "sFloat32");
        node.SetAttribute("alt_encoding", altEncoding);
        node.SetAttribute("precision", "string");
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

    public static IElement GenerateCGuid(this IXmlDocument document)
    {
        var guid = Guid.NewGuid();
        var ulongs = GetUuidLongs(guid);
        var cGuid = document.CreateElement("cGUID");
        var uuid = document.CreateElement("UUID");

        var e1 = document.CreateElement("e");
        e1.SetAttribute("type", "sUInt64");
        e1.SetTextContent(ulongs.Item1.ToString());

        var e2 = document.CreateElement("e");
        e2.SetAttribute("type", "sUInt64");
        e2.SetTextContent(ulongs.Item2.ToString());

        uuid.AppendNodes(e1, e2);

        var devString = document.CreateElement("DevString");
        devString.SetAttribute("type", "cDeltaString");
        devString.SetTextContent(guid.ToString());

        cGuid.AppendNodes(uuid, devString);
        return cGuid;
    }
}
