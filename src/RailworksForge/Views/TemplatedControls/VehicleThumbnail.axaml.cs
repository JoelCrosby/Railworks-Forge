using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace RailworksForge.Views.TemplatedControls;

public class VehicleThumbnail : TemplatedControl
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<VehicleThumbnail, IImage?>(nameof(Source));

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
}
