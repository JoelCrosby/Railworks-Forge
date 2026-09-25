using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Xml;

using AngleSharp.Dom;

namespace RailworksForge.Core.External;

public class SerzInternal
{
    private const string DeltaNamespace = "http://www.kuju.com/TnT/2003/Delta";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private readonly byte[] _data;
    private readonly List<string> _strings = [];
    private readonly Chunk?[] _chunks = new Chunk[255];
    private int _position;
    private int _nextChunk;

    public SerzInternal(ref byte[] data)
    {
        _data = data;
    }

    public static IDocument Convert(string inputPath)
    {
        var input = File.ReadAllBytes(inputPath);

        return new SerzInternal(ref input).ToXml();
    }

    public IDocument ToXml()
    {
        _position = 0;
        _nextChunk = 0;
        _strings.Clear();
        Array.Clear(_chunks);

        if (!ReadBytes(8).SequenceEqual("SERZ\0\0\x01\0"u8))
        {
            throw InvalidData("Unsupported SERZ header");
        }

        using var output = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };
        using (var writer = XmlWriter.Create(output, settings))
        {
            writer.WriteStartDocument();
            var parents = new Stack<string>();
            var hasRoot = false;

            while (_position < _data.Length)
            {
                var chunk = ReadChunk();

                switch (chunk.Kind)
                {
                    case 'C':
                        ReadBytes(5);
                        break;
                    case 'P':
                        var id = BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));
                        ReadUInt32();
                        writer.WriteStartElement(XmlName(chunk.Name));

                        if (!hasRoot)
                        {
                            writer.WriteAttributeString("xmlns", "d", null, DeltaNamespace);
                            Attribute(writer, "version", "1.0");
                            hasRoot = true;
                        }

                        if (id != 0)
                        {
                            Attribute(writer, "id", id.ToString(Invariant));
                        }

                        parents.Push(chunk.Name);
                        break;
                    case 'p':
                        var hasParent = parents.TryPop(out var parentName);
                        var closesCurrentParent = hasParent && parentName == chunk.Name;

                        if (!closesCurrentParent)
                        {
                            throw InvalidData("Mismatched closing element");
                        }

                        writer.WriteEndElement();
                        break;
                    case 'V':
                        writer.WriteStartElement(XmlName(chunk.Name));
                        Attribute(writer, "type", chunk.Type);
                        var isFloat = chunk.Type is "sFloat32" or "sFloat64";

                        if (isFloat)
                        {
                            var value = ReadFloat(chunk.Type);
                            var bits = new byte[8];
                            BinaryPrimitives.WriteDoubleLittleEndian(bits, value);
                            Attribute(writer, "alt_encoding", System.Convert.ToHexString(bits));
                            Attribute(writer, "precision", "string");
                            writer.WriteString(FormatFloat(value, "G6"));
                        }
                        else
                        {
                            writer.WriteString(ReadValue(chunk.Type));
                        }

                        writer.WriteEndElement();
                        break;
                    case 'A':
                        var count = ReadByte();
                        writer.WriteStartElement(XmlName(chunk.Name));
                        Attribute(writer, "numElements", count.ToString(Invariant));
                        Attribute(writer, "elementType", chunk.Type);
                        Attribute(writer, "precision", "string");
                        var values = new string[count];

                        for (var i = 0; i < count; i++)
                        {
                            values[i] = ReadValue(chunk.Type);
                        }

                        writer.WriteString(string.Join(" ", values));
                        writer.WriteEndElement();
                        break;
                    case 'R':
                        writer.WriteStartElement(XmlName(chunk.Name));
                        Attribute(writer, "type", "ref");
                        writer.WriteString(BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4)).ToString(Invariant));
                        writer.WriteEndElement();
                        break;
                    case 'N':
                        writer.WriteStartElement("d", "nil", DeltaNamespace);
                        writer.WriteEndElement();
                        break;
                    case 'B':
                        var size = ReadLength();
                        var bytes = ReadBytes(size);
                        writer.WriteStartElement("d", "blob", DeltaNamespace);
                        Attribute(writer, "size", size.ToString(Invariant));
                        WriteBlob(writer, bytes);
                        writer.WriteEndElement();
                        break;
                    default:
                        throw InvalidData($"Unsupported chunk '{chunk.Kind}'");
                }
            }

            var isComplete = hasRoot && parents.Count == 0;

            if (!isComplete)
            {
                throw InvalidData("Incomplete XML document");
            }

            writer.WriteEndDocument();
        }

        output.Position = 0;

        return XmlParser.ParseDocument(output);
    }

    private static void WriteBlob(XmlWriter writer, ReadOnlySpan<byte> bytes)
    {
        for (var offset = 0; offset < bytes.Length; offset += 8)
        {

            if (offset != 0)
            {
                var separator = offset % 32 == 0 ? "\n" : " ";
                writer.WriteString(separator);
            }

            var count = Math.Min(8, bytes.Length - offset);
            var hex = System.Convert.ToHexString(bytes.Slice(offset, count));
            writer.WriteString(hex);
        }
    }

    private Chunk ReadChunk()
    {
        var index = ReadByte();

        if (index != byte.MaxValue)
        {
            return _chunks[index] ?? throw InvalidData($"Unknown chunk index {index}");
        }

        var kind = (char)ReadByte();
        var hasName = kind is 'P' or 'p' or 'V' or 'A' or 'R';
        var name = hasName ? ReadString() : string.Empty;
        var hasType = kind is 'V' or 'A';
        var type = hasType ? ReadString() : string.Empty;
        var chunk = new Chunk(kind, name, type);
        _chunks[_nextChunk] = chunk;
        _nextChunk = (_nextChunk + 1) % _chunks.Length;

        return chunk;
    }

    private string ReadString()
    {
        var index = BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));

        if (index != ushort.MaxValue)
        {

            if (index >= _strings.Count)
            {
                throw InvalidData($"Unknown string index {index}");
            }

            return _strings[index];
        }

        var count = ReadLength();
        var start = _position;

        // SERZ counts Unicode characters rather than UTF-8 bytes.
        for (var i = 0; i < count; i++)
        {
            var status = Rune.DecodeFromUtf8(_data.AsSpan(_position), out _, out var consumed);

            if (status != System.Buffers.OperationStatus.Done)
            {
                throw InvalidData("Invalid or truncated UTF-8 string");
            }

            _position += consumed;
        }

        var value = Encoding.UTF8.GetString(_data, start, _position - start);
        _strings.Add(value);

        return value;
    }

    private string ReadValue(string type)
    {
        return type switch
        {
            "cDeltaString" => ReadString(),
            "bool" => ReadByte() == 0 ? "0" : "1",
            "sInt8" => unchecked((sbyte)ReadByte()).ToString(Invariant),
            "sUInt8" => ReadByte().ToString(Invariant),
            "sInt16" => BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2)).ToString(Invariant),
            "sUInt16" => BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2)).ToString(Invariant),
            "sInt32" => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4)).ToString(Invariant),
            "sUInt32" => ReadUInt32().ToString(Invariant),
            "sInt64" => BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(8)).ToString(Invariant),
            "sUInt64" => BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(8)).ToString(Invariant),
            "sFloat32" or "sFloat64" => FormatFloat(ReadFloat(type), "F7"),
            _ => throw InvalidData($"Unsupported value type '{type}'"),
        };
    }

    private static string FormatFloat(double value, string format)
    {

        if (double.IsPositiveInfinity(value))
        {
            return "inf";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-inf";
        }

        return value.ToString(format, Invariant).ToLowerInvariant();
    }

    private double ReadFloat(string type)
    {

        if (type == "sFloat32")
        {
            return BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(4));
        }

        return BinaryPrimitives.ReadDoubleLittleEndian(ReadBytes(8));
    }

    private byte ReadByte()
    {
        return ReadBytes(1)[0];
    }

    private uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));
    }

    private int ReadLength()
    {
        var length = ReadUInt32();

        if (length > _data.Length - _position)
        {
            throw InvalidData("Length exceeds remaining input");
        }

        return (int)length;
    }

    private ReadOnlySpan<byte> ReadBytes(int count)
    {

        if (count > _data.Length - _position)
        {
            throw InvalidData("Unexpected end of input");
        }

        var bytes = _data.AsSpan(_position, count);
        _position += count;

        return bytes;
    }

    private InvalidDataException InvalidData(string message)
    {
        return new InvalidDataException($"{message} at SERZ byte offset {_position}.");
    }

    private static string XmlName(string name)
    {

        if (name.Length == 0)
        {
            return "e";
        }

        return name.Replace("::", "-", StringComparison.Ordinal);
    }

    private static void Attribute(XmlWriter writer, string name, string value)
    {
        writer.WriteAttributeString("d", name, DeltaNamespace, value);
    }

    private sealed record Chunk(char Kind, string Name, string Type);
}
