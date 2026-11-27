using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;

namespace RailworksForge.Services;

public class ThemeService : IThemeService
{
    private static IPlatformSettings? _pinnedPlatformSettings;

    public void Apply(string theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        StopFollowingPinnedTheme();

        Application.Current.RequestedThemeVariant = ToThemeVariant(theme);
    }

    // Pins a system theme to the desktop's current preference until the platform reports its own colour values,
    // then follows the system again.
    public void ApplyAtStartup(string theme)
    {
        var application = Application.Current;
        var requested = ToThemeVariant(theme);
        var followsSystem = requested == ThemeVariant.Default;
        var platformSettings = application?.PlatformSettings;
        var systemPreference = followsSystem && platformSettings is not null
            ? SystemColorScheme.ReadFreedesktopPreference()
            : null;

        Apply(theme);

        if (application is null || platformSettings is null || systemPreference is null)
        {
            return;
        }

        application.RequestedThemeVariant = systemPreference;
        platformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
        _pinnedPlatformSettings = platformSettings;
    }

    private static void OnPlatformColorValuesChanged(object? sender, PlatformColorValues values)
    {
        Dispatcher.UIThread.Post(FollowSystemTheme);
    }

    private static void FollowSystemTheme()
    {
        var isStillPinned = _pinnedPlatformSettings is not null;

        StopFollowingPinnedTheme();

        if (isStillPinned && Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
        }
    }

    private static void StopFollowingPinnedTheme()
    {
        if (_pinnedPlatformSettings is null)
        {
            return;
        }

        _pinnedPlatformSettings.ColorValuesChanged -= OnPlatformColorValuesChanged;
        _pinnedPlatformSettings = null;
    }

    private static ThemeVariant ToThemeVariant(string theme)
    {
        return theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
