using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace RailworksForge.Views.Components;

public class SettingsRow : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SettingsRow, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsRow, string?>(nameof(Description));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<SettingsRow, object?>(nameof(Content));

    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<SettingsRow, object?>(nameof(Footer));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }
}
