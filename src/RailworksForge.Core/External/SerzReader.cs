using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace RailworksForge.Core.External;

public enum SerzNodeKind
{
    Open,
    Close,
    Value,
    Array,
    Reference,
    Nil,
    Blob,
}

public sealed class SerzReader
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private readonly byte[] _data;
    private readonly List<string> _strings = [];
    private readonly Chunk?[] _chunks = new Chunk[255];
    private int _position;
    private int _nextChunk;
    private bool _hasHeader;

    public SerzReader(byte[] data)
    {
        _data = data;
    }

    public SerzNodeKind Kind { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public int Id { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public bool IsFloat { get; private set; }

    public double FloatValue { get; private set; }

    public string[] Values { get; private set; } = [];

    public ReadOnlyMemory<byte> Blob { get; private set; }

    public bool Read()
    {

        if (!_hasHeader)
        {
            ReadHeader();
        }

        while (_position < _data.Length)
        {
            var chunk = ReadChunk();

            if (chunk.Kind == 'C')
            {
                ReadBytes(5);
                continue;
            }

            ReadNode(chunk);

            return true;
        }

        return false;
    }

    public InvalidDataException InvalidData(string message)
    {
        return new InvalidDataException($"{message} at SERZ byte offset {_position}.");
    }

    public static string FormatFloat(double value, string format)
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

    private void ReadHeader()
    {

        if (!ReadBytes(8).SequenceEqual("SERZ\0\0\x01\0"u8))
        {
            throw InvalidData("Unsupported SERZ header");
        }

        _hasHeader = true;
    }

    private void ReadNode(Chunk chunk)
    {
        Name = chunk.Name;
        Type = chunk.Type;
        Id = 0;
        Value = string.Empty;
        IsFloat = false;
        FloatValue = 0;
        Values = [];
        Blob = ReadOnlyMemory<byte>.Empty;

        switch (chunk.Kind)
        {
            case 'P':
                Kind = SerzNodeKind.Open;
                Id = BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));
                ReadUInt32();
                break;
            case 'p':
                Kind = SerzNodeKind.Close;
                break;
            case 'V':
                Kind = SerzNodeKind.Value;
                IsFloat = chunk.Type is "sFloat32" or "sFloat64";

                if (IsFloat)
                {
                    FloatValue = ReadFloat(chunk.Type);
                }
                else
                {
                    Value = ReadValue(chunk.Type);
                }

                break;
            case 'A':
                Kind = SerzNodeKind.Array;
                var count = ReadByte();
                var values = new string[count];

                for (var i = 0; i < count; i++)
                {
                    values[i] = ReadValue(chunk.Type);
                }

                Values = values;
                break;
            case 'R':
                Kind = SerzNodeKind.Reference;
                Value = BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4)).ToString(Invariant);
                break;
            case 'N':
                Kind = SerzNodeKind.Nil;
                break;
            case 'B':
                Kind = SerzNodeKind.Blob;
                var size = ReadLength();
                var start = _position;
                ReadBytes(size);
                Blob = _data.AsMemory(start, size);
                break;
            default:
                throw InvalidData($"Unsupported chunk '{chunk.Kind}'");
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

    private sealed record Chunk(char Kind, string Name, string Type);
}
