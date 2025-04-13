using Avalonia;
using Avalonia.Controls.Primitives;

namespace RailworksForge.Views.TemplatedControls;

public class NameValueDisplay : TemplatedControl
{
    public static readonly StyledProperty<string> HeadingProperty =
        AvaloniaProperty.Register<TableHeader, string>(nameof(Heading));

    public string Heading
    {
        get => GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<TableHeader, string?>(nameof(Value));

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
