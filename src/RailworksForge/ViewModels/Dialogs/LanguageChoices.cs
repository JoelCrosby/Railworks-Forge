using System;
using System.Collections.Generic;
using System.Linq;

using RailworksForge.Core.Common;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

// The language chips shared by the details dialogs.
public sealed class LanguageChoices
{
    public IReadOnlyList<LanguageOptionViewModel> Options { get; } = LocalisedText.Languages
        .Select(language => new LanguageOptionViewModel(language, Label(language)))
        .ToList();

    public void Select(GameLanguage language)
    {
        foreach (var option in Options)
        {
            option.IsSelected = option.Language == language;
        }
    }

    public void MarkText(Func<GameLanguage, bool> hasText)
    {
        foreach (var option in Options)
        {
            option.HasText = hasText(option.Language);
        }
    }

    public static string Label(GameLanguage language)
    {
        var label = language switch
        {
            GameLanguage.English => Strings.game_language_english,
            GameLanguage.French => Strings.game_language_french,
            GameLanguage.Italian => Strings.game_language_italian,
            GameLanguage.German => Strings.game_language_german,
            GameLanguage.Spanish => Strings.game_language_spanish,
            GameLanguage.Dutch => Strings.game_language_dutch,
            GameLanguage.Polish => Strings.game_language_polish,
            GameLanguage.Russian => Strings.game_language_russian,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
        };

        return label.CurrentValue;
    }
}
