using Avalonia;
using Avalonia.Styling;

namespace RailworksForge.Services;

public class ThemeService
{
    public void Apply(string theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
