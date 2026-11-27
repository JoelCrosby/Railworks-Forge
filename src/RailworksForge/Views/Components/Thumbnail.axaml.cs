using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

using LucideAvalonia.Enum;

namespace RailworksForge.Views.Components;

public class Thumbnail : TemplatedControl
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<Thumbnail, IImage?>(nameof(Source));

    public static readonly StyledProperty<LucideIconNames> PlaceholderIconProperty =
        AvaloniaProperty.Register<Thumbnail, LucideIconNames>(nameof(PlaceholderIcon), LucideIconNames.TrainFront);

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public LucideIconNames PlaceholderIcon
    {
        get => GetValue(PlaceholderIconProperty);
        set => SetValue(PlaceholderIconProperty, value);
    }
}
