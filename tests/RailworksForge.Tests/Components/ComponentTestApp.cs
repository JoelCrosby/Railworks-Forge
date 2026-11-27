using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace RailworksForge.Tests.Components;

// Loads only the theme, tokens and component styles, so component tests don't read the user's settings.
public class ComponentTestApp : Application
{
    private static readonly Uri BaseUri = new("avares://RailworksForge/");

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<ComponentTestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }

    public override void Initialize()
    {
        var tokens = new ResourceInclude(BaseUri) { Source = new Uri("avares://RailworksForge/Styles/Tokens.axaml") };
        var controls = new StyleInclude(BaseUri) { Source = new Uri("avares://RailworksForge/Styles/Controls.axaml") };
        var components = new StyleInclude(BaseUri) { Source = new Uri("avares://RailworksForge/Styles/Components.axaml") };

        Resources.MergedDictionaries.Add(tokens);
        Styles.Add(new FluentTheme());
        Styles.Add(controls);
        Styles.Add(components);
    }
}
