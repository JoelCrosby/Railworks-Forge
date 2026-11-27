using System.Buffers.Binary;
using System.Globalization;

using AngleSharp.Dom;

using RailworksForge.Core.Common;

namespace RailworksForge.Core.Extensions;

// Reads and writes the top-level fields of a ScenarioProperties.xml or RouteProperties.xml document. Fields are
// matched as direct children of the root, since nested elements such as a service's StartLocation share their names.
public static class PropertiesDocumentExtensions
{
    private const string LocalisedStringName = "Localisation-cUserLocalisedString";
    private const string DeltaStringType = "cDeltaString";

    public static IElement? ChildElement(this IElement parent, string name)
    {
        return parent.Children.FirstOrDefault(child => child.LocalName == name);
    }

    public static string ReadValue(this IElement root, string name)
    {
        return root.ChildElement(name)?.TextContent.Trim() ?? string.Empty;
    }

    public static int ReadInteger(this IElement root, string name)
    {
        _ = int.TryParse(root.ReadValue(name), CultureInfo.InvariantCulture, out var value);

        return value;
    }

    public static double ReadDouble(this IElement root, string name)
    {
        _ = double.TryParse(root.ReadValue(name), CultureInfo.InvariantCulture, out var value);

        return value;
    }

    public static ulong ReadUnsignedLong(this IElement root, string name)
    {
        _ = ulong.TryParse(root.ReadValue(name), CultureInfo.InvariantCulture, out var value);

        return value;
    }

    // The text is read as written, line breaks included, unlike SelectLocalisedStringContent which is for display.
    public static LocalisedText ReadLocalisedText(this IElement root, string name)
    {
        var localised = root.ChildElement(name)?.ChildElement(LocalisedStringName);

        if (localised is null)
        {
            return LocalisedText.Empty;
        }

        return LocalisedText.Languages.Aggregate(LocalisedText.Empty, (text, language) =>
        {
            var slot = localised.ChildElement(language.ToString());

            return slot is null ? text : text.With(language, slot.TextContent);
        });
    }

    // Only the language slots are written; the string's Key and any Other language pairs are left as they are.
    public static void WriteLocalisedText(this IElement root, string name, LocalisedText text, string precedingName)
    {
        var field = root.ChildElement(name);

        if (field is null && !text.HasAnyText)
        {
            return;
        }

        var localised = field?.ChildElement(LocalisedStringName) ?? CreateLocalisedField(root, name, precedingName);

        foreach (var language in LocalisedText.Languages)
        {
            var slot = localised.ChildElement(language.ToString());
            var value = text[language];

            if (slot is not null)
            {
                slot.SetTextContent(value);
            }
            else if (value.Length > 0)
            {
                InsertLanguageSlot(localised, language, value);
            }
        }
    }

    public static void WriteValue(this IElement root, string name, string type, string value)
    {
        var element = root.ChildElement(name) ?? AppendTypedElement(root, name, type);

        element.SetTextContent(value);
    }

    public static void WriteInteger(this IElement root, string name, int value)
    {
        root.WriteValue(name, "sInt32", value.ToString(CultureInfo.InvariantCulture));
    }

    // alt_encoding holds the value's exact bits and can be read in place of the text, so a stale one would undo the edit.
    public static void WriteFloat(this IElement root, string name, double value)
    {
        var element = root.ChildElement(name) ?? AppendTypedElement(root, name, "sFloat32");

        element.SetAttribute(Utilities.NS, "d:alt_encoding", EncodeFloat(value));
        element.SetAttribute(Utilities.NS, "d:precision", "string");
        element.SetTextContent(value.ToString("R", CultureInfo.InvariantCulture));
    }

    public static string EncodeFloat(double value)
    {
        Span<byte> bits = stackalloc byte[sizeof(double)];
        BinaryPrimitives.WriteDoubleLittleEndian(bits, value);

        return Convert.ToHexString(bits);
    }

    private static IElement AppendTypedElement(IElement root, string name, string type)
    {
        var element = root.Owner!.CreateXmlElement(name);
        element.SetAttribute(Utilities.NS, "d:type", type);
        root.AppendChild(element);

        return element;
    }

    private static IElement CreateLocalisedField(IElement root, string name, string precedingName)
    {
        var document = root.Owner!;
        var field = document.CreateXmlElement(name);
        var localised = document.CreateXmlElement(LocalisedStringName);

        foreach (var language in LocalisedText.Languages)
        {
            localised.AppendChild(CreateDeltaString(document, language.ToString(), string.Empty));
        }

        localised.AppendChild(document.CreateXmlElement("Other"));
        localised.AppendChild(CreateDeltaString(document, "Key", Guid.NewGuid().ToString()));
        field.AppendChild(localised);

        var preceding = root.ChildElement(precedingName);

        if (preceding is not null)
        {
            preceding.After(field);
        }
        else
        {
            root.AppendChild(field);
        }

        return localised;
    }

    private static void InsertLanguageSlot(IElement localised, GameLanguage language, string value)
    {
        var slot = CreateDeltaString(localised.Owner!, language.ToString(), value);
        var laterLanguages = LocalisedText.Languages.Where(other => other > language).Select(other => other.ToString());
        var followingNames = laterLanguages.Concat(["Other", "Key"]).ToHashSet();
        var following = localised.Children.FirstOrDefault(child => followingNames.Contains(child.LocalName));

        if (following is not null)
        {
            following.Before(slot);
        }
        else
        {
            localised.AppendChild(slot);
        }
    }

    private static IElement CreateDeltaString(IDocument document, string name, string value)
    {
        var element = document.CreateXmlElement(name);
        element.SetAttribute(Utilities.NS, "d:type", DeltaStringType);
        element.SetTextContent(value);

        return element;
    }
}
