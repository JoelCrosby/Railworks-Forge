using System.Globalization;
using System.Text.RegularExpressions;

using Echoes;

using RailworksForge.Translations;

namespace RailworksForge.UnitTests;

public partial class TranslationTests
{
    private static readonly Dictionary<string, string> English = ReadTranslations("Strings.toml");

    public static TheoryData<string> LocaleFiles => ["Strings_de.toml", "Strings_es.toml", "Strings_fr.toml"];

    [Theory]
    [MemberData(nameof(LocaleFiles))]
    public void Locale_CoversEveryEnglishKey(string fileName)
    {
        var locale = ReadTranslations(fileName);

        Assert.True(English.Count > 100, $"only read {English.Count} English translations");

        var missingFromLocale = English.Keys.Except(locale.Keys).ToList();
        var missingFromEnglish = locale.Keys.Except(English.Keys).ToList();

        Assert.Empty(missingFromLocale);
        Assert.Empty(missingFromEnglish);
    }

    // string.Format throws when a translation references a placeholder the caller doesn't supply.
    [Theory]
    [MemberData(nameof(LocaleFiles))]
    public void Locale_UsesTheSamePlaceholders(string fileName)
    {
        var locale = ReadTranslations(fileName);

        var mismatched = English
            .Where(entry => locale.TryGetValue(entry.Key, out var translation) && !HaveSamePlaceholders(entry.Value, translation))
            .Select(entry => entry.Key)
            .ToList();

        Assert.Empty(mismatched);
    }

    [Theory]
    [InlineData("es-ES", "Rutas")]
    [InlineData("fr-FR", "Itinéraires")]
    public void SelectedCulture_ResolvesItsLocaleFile(string culture, string expectedRoutes)
    {
        var previous = TranslationProvider.Culture;

        try
        {
            TranslationProvider.SetCulture(CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expectedRoutes, Strings.routes.CurrentValue);
        }
        finally
        {
            TranslationProvider.SetCulture(previous);
        }
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
