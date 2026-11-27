using Avalonia;
using Avalonia.Controls.Primitives;

using RailworksForge.Core.Assets;

namespace RailworksForge.Views.Components;

public class AcquisitionStateIcon : TemplatedControl
{
    public static readonly StyledProperty<AcquisitionState> StateProperty =
        AvaloniaProperty.Register<AcquisitionStateIcon, AcquisitionState>(nameof(State));

    public AcquisitionState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
}
