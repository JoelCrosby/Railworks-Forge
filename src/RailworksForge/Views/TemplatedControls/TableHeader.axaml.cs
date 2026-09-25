using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace RailworksForge.Views.TemplatedControls;

public class TableHeader : TemplatedControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TableHeader, string>(nameof(Text));

    public static readonly StyledProperty<object?> ToolsProperty =
        AvaloniaProperty.Register<TableHeader, object?>(nameof(Tools));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    [Content]
    public object? Tools
    {
        get => GetValue(ToolsProperty);
        set => SetValue(ToolsProperty, value);
    }
}
