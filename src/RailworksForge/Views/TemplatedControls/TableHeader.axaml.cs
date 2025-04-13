using Avalonia;
using Avalonia.Controls.Primitives;

namespace RailworksForge.Views.TemplatedControls;

public class TableHeader : TemplatedControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TableHeader, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
