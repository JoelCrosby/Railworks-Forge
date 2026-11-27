using Avalonia;
using Avalonia.Controls.Primitives;

namespace RailworksForge.Views.Components;

public class TaskIndicator : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<TaskIndicator, string?>(nameof(Label));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<TaskIndicator, double>(nameof(Progress));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<TaskIndicator, bool>(nameof(IsIndeterminate), true);

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }
}
