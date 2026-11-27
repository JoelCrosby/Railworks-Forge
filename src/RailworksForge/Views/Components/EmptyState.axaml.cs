using Avalonia;
using Avalonia.Controls.Primitives;

using LucideAvalonia.Enum;

namespace RailworksForge.Views.Components;

public class EmptyState : TemplatedControl
{
    public static readonly StyledProperty<LucideIconNames> IconProperty =
        AvaloniaProperty.Register<EmptyState, LucideIconNames>(nameof(Icon), LucideIconNames.Inbox);

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Title));

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Hint));

    public LucideIconNames Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }
}
