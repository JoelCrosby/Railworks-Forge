using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace RailworksForge.Views.Components;

public class RouteCard : TemplatedControl
{
    public static readonly StyledProperty<IImage?> ImageProperty =
        AvaloniaProperty.Register<RouteCard, IImage?>(nameof(Image));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<RouteCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<RouteCard, string?>(nameof(Subtitle));

    public IImage? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
}
