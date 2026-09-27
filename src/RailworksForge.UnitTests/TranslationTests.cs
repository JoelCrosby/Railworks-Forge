using System.Text.RegularExpressions;

using RailworksForge.Translations;

namespace RailworksForge.UnitTests;

public partial class TranslationTests
{
    private static readonly Dictionary<string, string> English = ReadTranslations("Strings.toml");
    private static readonly Dictionary<string, string> German = ReadTranslations("Strings_de.toml");

    [Fact]
    public void GermanTranslations_CoverEveryEnglishKey()
    {
        Assert.True(English.Count > 100, $"only read {English.Count} English translations");

        var missingFromGerman = English.Keys.Except(German.Keys).ToList();
        var missingFromEnglish = German.Keys.Except(English.Keys).ToList();

        Assert.Empty(missingFromGerman);
        Assert.Empty(missingFromEnglish);
    }

    // string.Format throws when a translation references a placeholder the caller doesn't supply.
    [Fact]
    public void GermanTranslations_UseTheSamePlaceholders()
    {
        var mismatched = English
            .Where(entry => German.TryGetValue(entry.Key, out var german) && !HaveSamePlaceholders(entry.Value, german))
            .Select(entry => entry.Key)
            .ToList();

        Assert.Empty(mismatched);
    }

    private static bool HaveSamePlaceholders(string first, string second)
    {
        var firstPlaceholders = Placeholder().Matches(first).Select(match => match.Value).Order();
        var secondPlaceholders = Placeholder().Matches(second).Select(match => match.Value).Order();

        return firstPlaceholders.SequenceEqual(secondPlaceholders);
    }

    private static Dictionary<string, string> ReadTranslations(string fileName)
    {
        var assembly = typeof(Strings).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith($".{fileName}"));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        return TranslationEntry()
            .Matches(content)
            .Where(match => !match.Groups["key"].Value.StartsWith("generated_"))
            .ToDictionary(match => match.Groups["key"].Value, match => match.Groups["value"].Value);
    }

    [GeneratedRegex("^(?<key>[a-z0-9_]+) = '(?<value>[^']*)'", RegexOptions.Multiline)]
    private static partial Regex TranslationEntry();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();
}
