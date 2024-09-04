using System.Collections;
using System.Text;
using System.Xml;

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

    public XmlDocument ToXml()
    {
        var xmlDoc = new XmlDocument();

        if (xmlDoc is not XmlNode currentXmlNode)
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
            else if (index1 <= _chunkCache.Count)
            {
                sChunk = _chunkCache.ElementAtOrDefault(index1);
                _chunkKind = sChunk.MKind;
                _chunkName = _stringCache.ElementAtOrDefault(sChunk.MNameIx);
                _typeName = _stringCache.ElementAtOrDefault(sChunk.MTypeNameIx) ?? string.Empty;
            }

            switch (_chunkKind)
            {
                case EChunkKind.Vector:
                    var num1 = (int)_mData[_dataIx++];
                    var str = "";

                    for (var index2 = 0; index2 < num1; ++index2)
                        str = str + ReadValue(_typeName) + " ";

                    var element1 = xmlDoc.CreateElement(_chunkName);
                    var textNode1 = xmlDoc.CreateTextNode(str.Trim());
                    element1.AppendChild(textNode1);
                    var attribute1 = xmlDoc.CreateAttribute("d:numElements");
                    attribute1.Value = num1.ToString();
                    element1.Attributes.Append(attribute1);
                    var attribute2 = xmlDoc.CreateAttribute("d:elementType");
                    attribute2.Value = _typeName;
                    element1.Attributes.Append(attribute2);
                    var attribute3 = xmlDoc.CreateAttribute("d:precision");
                    attribute3.Value = "string";
                    element1.Attributes.Append(attribute3);
                    currentXmlNode.AppendChild(element1);
                    continue;
                case EChunkKind.Control:
                    _dataIx += 5;
                    continue;
                case EChunkKind.BeginParent:
                    ++_parentCount;
                    var num2 = ReadInt32();
                    _childCount = ReadInt32();
                    XmlNode element2 = xmlDoc.CreateElement(_chunkName.Replace("::", "-"));

                    if (num2 != 0)
                    {
                        var attribute4 = xmlDoc.CreateAttribute("d:id");
                        attribute4.Value = num2.ToString();
                        element2.Attributes.Append(attribute4);
                    }

                    currentXmlNode.AppendChild(element2);
                    currentXmlNode = element2;
                    continue;
                case EChunkKind.Value:
                    XmlNode element3 = xmlDoc.CreateElement(_chunkName.Length == 0 ? "e" : _chunkName);
                    var textNode2 = xmlDoc.CreateTextNode(ReadValue(_typeName));
                    var attribute5 = xmlDoc.CreateAttribute("d", "type", null);
                    attribute5.Value = _typeName;
                    element3.Attributes.Append(attribute5);
                    element3.AppendChild(textNode2);
                    currentXmlNode.AppendChild(element3);
                    continue;
                case EChunkKind.EndParent:
                    currentXmlNode = currentXmlNode.ParentNode;
                    --_parentCount;
                    continue;
                default:
                    Console.WriteLine("ERK " + _chunkKind);
                    continue;
            }
        }

        return xmlDoc;
    }

    private string ReadString()
    {
        var utF8 = Encoding.UTF8;
        var index = _mData[_dataIx++] | (_mData[_dataIx++] << 8);

        string? str;

        if (index == ushort.MaxValue)
        {
            var count = ReadInt32();
            str = utF8.GetString(_mData, _dataIx, count);
            _dataIx += count;

            _stringCache.Add(str);

            _lastStringCacheIx = _stringCache.Count;
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

    private string? ReadValue(string? deltaType)
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
                return single.ToString();
            case "sInt32":
                return ReadInt32().ToString();
            case "bool":
                var boolean = BitConverter.ToBoolean(_mData, _dataIx).ToString();
                _dataIx += 1;
                return boolean;
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
        EndOfFile = 256, // 0x00000100
    }

    private struct SChunk
    {
        public EChunkKind MKind;
        public int MNameIx;
        public int MTypeNameIx;
    }
}
