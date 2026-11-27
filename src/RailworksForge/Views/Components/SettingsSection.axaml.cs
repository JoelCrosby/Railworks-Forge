using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace RailworksForge.Views.Components;

public class SettingsSection : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SettingsSection, string?>(nameof(Title));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<SettingsSection, object?>(nameof(Content));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
}
