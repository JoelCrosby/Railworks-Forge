using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace RailworksForge.Views.Components;

[PseudoClasses(":flipped")]
public class VehicleCard : TemplatedControl
{
    public static readonly StyledProperty<IImage?> ImageProperty =
        AvaloniaProperty.Register<VehicleCard, IImage?>(nameof(Image));

    public static readonly StyledProperty<string?> PositionProperty =
        AvaloniaProperty.Register<VehicleCard, string?>(nameof(Position));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<VehicleCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> CaptionProperty =
        AvaloniaProperty.Register<VehicleCard, string?>(nameof(Caption));

    public static readonly StyledProperty<bool> IsFlippedProperty =
        AvaloniaProperty.Register<VehicleCard, bool>(nameof(IsFlipped));

    public IImage? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public string? Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public bool IsFlipped
    {
        get => GetValue(IsFlippedProperty);
        set => SetValue(IsFlippedProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsFlippedProperty)
        {
            PseudoClasses.Set(":flipped", IsFlipped);
        }
    }
}
