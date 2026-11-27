using Avalonia;
using Avalonia.Controls.Primitives;

namespace RailworksForge.Views.Components;

public class MarkerLabel : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MarkerLabel, string?>(nameof(Text));

    public static readonly StyledProperty<bool> IsMarkedProperty =
        AvaloniaProperty.Register<MarkerLabel, bool>(nameof(IsMarked));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsMarked
    {
        get => GetValue(IsMarkedProperty);
        set => SetValue(IsMarkedProperty, value);
    }
}
