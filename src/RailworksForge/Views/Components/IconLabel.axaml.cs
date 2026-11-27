using Avalonia;
using Avalonia.Controls.Primitives;

using LucideAvalonia.Enum;

namespace RailworksForge.Views.Components;

public class IconLabel : TemplatedControl
{
    public static readonly StyledProperty<LucideIconNames> IconProperty =
        AvaloniaProperty.Register<IconLabel, LucideIconNames>(nameof(Icon));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<IconLabel, string?>(nameof(Text));

    public LucideIconNames Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
