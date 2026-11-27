using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace RailworksForge.Views.Components;

public class SectionHeader : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SectionHeader, string?>(nameof(Title));

    public static readonly StyledProperty<int?> CountProperty =
        AvaloniaProperty.Register<SectionHeader, int?>(nameof(Count));

    public static readonly StyledProperty<object?> ToolsProperty =
        AvaloniaProperty.Register<SectionHeader, object?>(nameof(Tools));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public int? Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    [Content]
    public object? Tools
    {
        get => GetValue(ToolsProperty);
        set => SetValue(ToolsProperty, value);
    }
}
