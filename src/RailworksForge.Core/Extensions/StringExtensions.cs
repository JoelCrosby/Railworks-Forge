using System.Text;
using System.Text.RegularExpressions;

namespace RailworksForge.Core.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex(@"\s", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[^a-z0-9\s-_]", RegexOptions.Compiled)]
    private static partial Regex AlphaNumericRegex();

    [GeneratedRegex("([-_]){2,}", RegexOptions.Compiled)]
    private static partial Regex DoubleDashesRegex();

    public static string ToUrlSlug(this string value)
    {
        var lower = value.ToLowerInvariant();
        var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(lower);
        var withoutAccents = Encoding.ASCII.GetString(bytes);
        var withoutSpaces = WhitespaceRegex().Replace(withoutAccents, "-");
        var withoutInvalidChars = AlphaNumericRegex().Replace(withoutSpaces, "");
        var trimmed = withoutInvalidChars.Trim('-', '_');
        var withoutDoubles = DoubleDashesRegex().Replace(trimmed, "$1");

        return withoutDoubles;
    }

    public static string NormalisePath(this string value)
    {
        return value.Normalize().ToLowerInvariant();
    }

    public static string ToRelativeGamePath(this string value)
    {
        var length = Paths.GetGameDirectory().Length;
        return value[length..];
    }
}
