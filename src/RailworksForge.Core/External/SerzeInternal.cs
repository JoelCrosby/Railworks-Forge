using System.Globalization;
using System.Text;

using AngleSharp.Dom;

using RailworksForge.Core.Extensions;


// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantAssignment

namespace RailworksForge.Core.External;

public class SerzInternal
{
    private int _childCount;
    private readonly List<SChunk> _chunkCache = new(1000);

    private EChunkKind _chunkKind;
    private string? _chunkName;
    private readonly byte[] _mData;
    private int _dataIx = 8;
    private int _lastChunkCacheIx;
    private int _lastStringCacheIx;
    private int _parentCount;
    private readonly List<string> _stringCache = new(1000);
    private string? _typeName;

    public SerzInternal(ref byte[] data)
    {
        _mData = data;
    }

    public IDocument ToXml()
    {
        var xmlDoc = XmlParser.ParseDocument("<root></root>");
        var currentXmlNode = xmlDoc.DocumentElement;

        if (currentXmlNode is null)
        {
            throw new Exception("could not create xml doc");
        }

        var chunkIx = 8;

        while (_dataIx < _mData.Length)
        {
            chunkIx = _dataIx;

            var index1 = (int)_mData[_dataIx++];

            SChunk sChunk;

            if (index1 == byte.MaxValue)
            {
                sChunk.MKind = _chunkKind = (EChunkKind)_mData[_dataIx++];

                if (_chunkKind != EChunkKind.Control)
                {
                    _chunkName = ReadString();
                }

                sChunk.MNameIx = _lastStringCacheIx;

                if (_chunkKind is EChunkKind.Value or EChunkKind.Vector)
                {
                    _typeName = ReadString();
                }

                sChunk.MTypeNameIx = _lastStringCacheIx;
                _chunkCache.Add(sChunk);

                _lastChunkCacheIx = _chunkCache.Count;
            }
            else
            {
                sChunk = _chunkCache.ElementAtOrDefault(index1);
                _chunkKind = sChunk.MKind;
                _chunkName = _stringCache.ElementAtOrDefault(sChunk.MNameIx);
                _typeName = _stringCache.ElementAtOrDefault(sChunk.MTypeNameIx) ?? string.Empty;
            }

            if (currentXmlNode is null)
            {
                throw new Exception("could get currentXmlNode");
            }

            switch (_chunkKind)
            {
                case EChunkKind.Vector:
                    var num1 = (int)_mData[_dataIx++];
                    var str = "";

                    for (var index2 = 0; index2 < num1; ++index2)
                        str = str + ReadValue(_typeName) + " ";

                    var element1 = xmlDoc.CreateXmlElement(_chunkName ?? "e");
                    element1.SetTextContent(str.Trim());
                    element1.SetAttribute("d:numElements", num1.ToString());
                    element1.SetAttribute("d:elementType", _typeName);
                    element1.SetAttribute("d:precision", "string");
                    currentXmlNode.AppendChild(element1);
                    continue;
                case EChunkKind.Control:
                    _dataIx += 5;
                    continue;
                case EChunkKind.BeginParent:
                    ++_parentCount;
                    var num2 = ReadInt32();
                    _childCount = ReadInt32();
                    var element2 = xmlDoc.CreateXmlElement(_chunkName!.Replace("::", "-"));

                    if (num2 != 0)
                    {
                        element2.SetAttribute("d:id", num2.ToString());
                    }

                    currentXmlNode.AppendChild(element2);
                    currentXmlNode = element2;
                    continue;
                case EChunkKind.Value:
                    var element3 = xmlDoc.CreateXmlElement(_chunkName?.Length == 0 ? "e" : _chunkName ?? "e");
                    element3.SetTextContent(ReadValue(_typeName));
                    element3.SetAttribute("d:type", _typeName);
                    currentXmlNode.AppendChild(element3);
                    continue;
                case EChunkKind.EndParent:
                    currentXmlNode = currentXmlNode.ParentElement ?? currentXmlNode;
                    --_parentCount;
                    continue;
                case EChunkKind.Nil:
                    var nilElement = xmlDoc.CreateXmlElement("d:nil");
                    currentXmlNode.AppendChild(nilElement);
                    currentXmlNode = currentXmlNode.ParentElement ?? currentXmlNode;
                    --_parentCount;
                    continue;
                default:
                    Console.WriteLine("ERK " + _chunkKind, _mData);
                    continue;
            }
        }

        return xmlDoc;
    }

    private string ReadString()
    {
        var index = _mData[_dataIx++] | (_mData[_dataIx++] << 8);

        string? str;

        if (index == ushort.MaxValue)
        {
            var count = ReadInt32();
            str = Encoding.UTF8.GetString(_mData, _dataIx, count);
            _dataIx += count;

            _stringCache.Add(str);

            _lastStringCacheIx = _stringCache.Count - 1;
        }
        else
        {
            str = _stringCache.ElementAtOrDefault(index) ?? string.Empty;
            _lastStringCacheIx = index;
        }

        return str;
    }

    private int ReadInt32()
    {
        return _mData[_dataIx++] | (_mData[_dataIx++] << 8) | (_mData[_dataIx++] << 16) | (_mData[_dataIx++] << 24);
    }

    private int ReadInt16()
    {
        return _mData[_dataIx++] | (_mData[_dataIx++] << 8);
    }

    private int ReadUInt8()
    {
        return _mData[_dataIx++];
    }

    private string ReadValue(string? deltaType)
    {
        switch (deltaType)
        {
            case "sUInt64":
                var num = (ulong)ReadInt32();
                return (((ulong)ReadInt32() << 32) | num).ToString();
            case "cDeltaString":
                return ReadString();
            case "sFloat32":
                var single = BitConverter.ToSingle(_mData, _dataIx);
                _dataIx += 4;
                return single.ToString(CultureInfo.InvariantCulture);
            case "sInt32":
                return ReadInt32().ToString();
            case "sUInt32":
                var num32 = (ulong)ReadInt16();
                return (((ulong)ReadInt16() << 32) | num32).ToString();
            case "sUInt8":
                var value = ReadUInt8().ToString();
                return value;
            case "bool":
                var boolean = BitConverter.ToBoolean(_mData, _dataIx);
                _dataIx += 1;
                return boolean ? "1" : "0";
            default:
                Console.WriteLine("unhandled value type: " + deltaType);
                return "ARSE";
        }
    }

    private enum EChunkKind
    {
        Vector = 65, // 0x00000041
        Blob = 66, // 0x00000042
        Control = 67, // 0x00000043
        Nil = 78, // 0x0000004E
        BeginParent = 80, // 0x00000050
        Reference = 82, // 0x00000052
        UnusedChunkCache = 85, // 0x00000055
        Value = 86, // 0x00000056
        EndParent = 112, // 0x00000070
        Unknown = 255,
        EndOfFile = 256, // 0x00000100
    }

    private struct SChunk
    {
        public EChunkKind MKind;
        public int MNameIx;
        public int MTypeNameIx;
    }
}
