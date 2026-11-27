using System.Collections.Immutable;

namespace RailworksForge.Core.Common;

// The order matches the slots in a Localisation-cUserLocalisedString element.
public enum GameLanguage
{
    English,
    French,
    Italian,
    German,
    Spanish,
    Dutch,
    Polish,
    Russian,
}

public sealed record LocalisedText
{
    public static LocalisedText Empty { get; } = new();

    public static IReadOnlyList<GameLanguage> Languages { get; } = Enum.GetValues<GameLanguage>();

    public ImmutableDictionary<GameLanguage, string> Values { get; init; } = ImmutableDictionary<GameLanguage, string>.Empty;

    public string this[GameLanguage language] => Values.GetValueOrDefault(language, string.Empty);

    public bool HasAnyText => Languages.Any(HasText);

    public GameLanguage? FirstLanguageWithText => Languages.Where(HasText).Cast<GameLanguage?>().FirstOrDefault();

    // The text the game falls back to when the player's language has none.
    public string DisplayText
    {
        get
        {
            var language = FirstLanguageWithText;
            var text = language is null ? string.Empty : this[language.Value];

            return text.Trim().ReplaceLineEndings(string.Empty);
        }
    }

    public bool HasText(GameLanguage language)
    {
        return !string.IsNullOrWhiteSpace(this[language]);
    }

    public LocalisedText With(GameLanguage language, string text)
    {
        return this with { Values = Values.SetItem(language, text) };
    }

    public static LocalisedText From(params (GameLanguage Language, string Text)[] values)
    {
        return values.Aggregate(Empty, (text, value) => text.With(value.Language, value.Text));
    }

    public bool Equals(LocalisedText? other)
    {
        if (other is null)
        {
            return false;
        }

        return Languages.All(language => this[language] == other[language]);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var language in Languages)
        {
            hash.Add(this[language]);
        }

        return hash.ToHashCode();
    }
}
