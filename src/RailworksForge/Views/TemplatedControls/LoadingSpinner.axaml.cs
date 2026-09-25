using Avalonia;
using Avalonia.Controls.Primitives;

namespace RailworksForge.Views.TemplatedControls;

public class LoadingSpinner : TemplatedControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<LoadingSpinner, bool>(nameof(IsActive), defaultValue: true);

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}
