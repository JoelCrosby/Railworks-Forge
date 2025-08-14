using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Subjects;

using Echoes;

namespace RailworksForge.Util;

public static class TranslationService
{
    private static TranslationProviderHandler Handler = new ();

    public static TextProvider GetProvider(string key) => Handler.AddProvider(key);
}

public class TranslationProviderHandler
{
    private List<TextProvider> TextProviders { get; } = [];

    public TranslationProviderHandler()
    {
        TranslationProvider.OnCultureChanged += TranslationProviderOnOnCultureChanged;
    }

    private void TranslationProviderOnOnCultureChanged(object? sender, CultureInfo e)
    {
        foreach (var provider in TextProviders)
        {
            provider.Update();
        }
    }

    public TextProvider AddProvider(string key)
    {
        var provider = new TextProvider(key);

        TextProviders.Add(provider);

        return provider;
    }
}

public class TextProvider(string key)
{
    public BehaviorSubject<string> Text { get; } = new(Utils.GetTranslation(key));

    private string Key { get; } = key;

    public void Update()
    {
        Text.OnNext(Utils.GetTranslation(Key));
    }
}
