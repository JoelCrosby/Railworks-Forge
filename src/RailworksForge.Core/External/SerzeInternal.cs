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

    public SerzInternal(ref byte[] data)
    {
        _data = data;
    }

    public static IDocument Convert(string inputPath)
    {
        var input = File.ReadAllBytes(inputPath);

        return new SerzInternal(ref input).ToXml();
    }

    public static void Convert(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = File.ReadAllBytes(inputPath);
        var converter = new SerzInternal(ref input);
        var temporaryPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                converter.WriteXml(output, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, outputPath, true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    public IDocument ToXml()
    {
        using var output = new MemoryStream();
        WriteXml(output, CancellationToken.None);
        output.Position = 0;

        return XmlParser.ParseDocument(output);
    }

    private void WriteXml(Stream output, CancellationToken cancellationToken)
    {
        var reader = new SerzReader(_data);
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            CheckCharacters = false,
        };

        using (var writer = XmlWriter.Create(output, settings))
        {
            writer.WriteStartDocument();
            var parents = new Stack<string>();
            var hasRoot = false;

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (reader.Kind)
                {
                    case SerzNodeKind.Open:
                        writer.WriteStartElement(XmlName(reader.Name));

                        if (!hasRoot)
                        {
                            writer.WriteAttributeString("xmlns", "d", null, DeltaNamespace);
                            Attribute(writer, "version", "1.0");
                            hasRoot = true;
                        }

                        if (reader.Id != 0)
                        {
                            Attribute(writer, "id", reader.Id.ToString(Invariant));
                        }

                        parents.Push(reader.Name);
                        break;
                    case SerzNodeKind.Close:
                        var hasParent = parents.TryPop(out var parentName);
                        var closesCurrentParent = hasParent && parentName == reader.Name;

                        if (!closesCurrentParent)
                        {
                            throw reader.InvalidData("Mismatched closing element");
                        }

                        writer.WriteEndElement();
                        break;
                    case SerzNodeKind.Value:
                        writer.WriteStartElement(XmlName(reader.Name));
                        Attribute(writer, "type", reader.Type);

                        if (reader.IsFloat)
                        {
                            var bits = new byte[8];
                            BinaryPrimitives.WriteDoubleLittleEndian(bits, reader.FloatValue);
                            Attribute(writer, "alt_encoding", System.Convert.ToHexString(bits));
                            Attribute(writer, "precision", "string");
                            writer.WriteString(SerzReader.FormatFloat(reader.FloatValue, "G6"));
                        }
                        else
                        {
                            WriteText(writer, reader.Value);
                        }

                        writer.WriteEndElement();
                        break;
                    case SerzNodeKind.Array:
                        writer.WriteStartElement(XmlName(reader.Name));
                        Attribute(writer, "numElements", reader.Values.Length.ToString(Invariant));
                        Attribute(writer, "elementType", reader.Type);
                        Attribute(writer, "precision", "string");
                        WriteText(writer, string.Join(" ", reader.Values));
                        writer.WriteEndElement();
                        break;
                    case SerzNodeKind.Reference:
                        writer.WriteStartElement(XmlName(reader.Name));
                        Attribute(writer, "type", "ref");
                        writer.WriteString(reader.Value);
                        writer.WriteEndElement();
                        break;
                    case SerzNodeKind.Nil:
                        writer.WriteStartElement("d", "nil", DeltaNamespace);
                        writer.WriteEndElement();
                        break;
                    case SerzNodeKind.Blob:
                        writer.WriteStartElement("d", "blob", DeltaNamespace);
                        Attribute(writer, "size", reader.Blob.Length.ToString(Invariant));
                        WriteBlob(writer, reader.Blob.Span);
                        writer.WriteEndElement();
                        break;
                }
            }

            var isComplete = hasRoot && parents.Count == 0;

            if (!isComplete)
            {
                throw reader.InvalidData("Incomplete XML document");
            }

            writer.WriteEndDocument();
        }
    }

    // serz64.exe writes characters that XML forbids, such as NUL, as raw bytes rather than rejecting or escaping them.
    // XmlWriter would emit &#x0; instead, which AngleSharp refuses to parse.
    private static void WriteText(XmlWriter writer, string text)
    {

        if (IsValidXmlText(text))
        {
            writer.WriteString(text);

            return;
        }

        var escaped = new StringBuilder(text.Length + 16);

        foreach (var character in text)
        {
            var entity = character switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => null,
            };

            if (entity is null)
            {
                escaped.Append(character);
            }
            else
            {
                escaped.Append(entity);
            }
        }

        writer.WriteRaw(escaped.ToString());
    }

    private static bool IsValidXmlText(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var isSurrogatePair = i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], text[i]);

            if (isSurrogatePair)
            {
                i++;
                continue;
            }

            if (!XmlConvert.IsXmlChar(text[i]))
            {
                return false;
            }
        }

        return true;
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
}
