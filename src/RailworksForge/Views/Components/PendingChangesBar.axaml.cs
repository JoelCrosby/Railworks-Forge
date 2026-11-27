using System.Windows.Input;

using Avalonia;
using Avalonia.Controls.Primitives;

namespace RailworksForge.Views.Components;

public class PendingChangesBar : TemplatedControl
{
    public static readonly StyledProperty<string?> SummaryProperty =
        AvaloniaProperty.Register<PendingChangesBar, string?>(nameof(Summary));

    public static readonly StyledProperty<string?> DetailsProperty =
        AvaloniaProperty.Register<PendingChangesBar, string?>(nameof(Details));

    public static readonly StyledProperty<string?> DiscardTextProperty =
        AvaloniaProperty.Register<PendingChangesBar, string?>(nameof(DiscardText));

    public static readonly StyledProperty<string?> ApplyTextProperty =
        AvaloniaProperty.Register<PendingChangesBar, string?>(nameof(ApplyText));

    public static readonly StyledProperty<ICommand?> DiscardCommandProperty =
        AvaloniaProperty.Register<PendingChangesBar, ICommand?>(nameof(DiscardCommand));

    public static readonly StyledProperty<ICommand?> ApplyCommandProperty =
        AvaloniaProperty.Register<PendingChangesBar, ICommand?>(nameof(ApplyCommand));

    public string? Summary
    {
        get => GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }

    public string? Details
    {
        get => GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }

    public string? DiscardText
    {
        get => GetValue(DiscardTextProperty);
        set => SetValue(DiscardTextProperty, value);
    }

    public string? ApplyText
    {
        get => GetValue(ApplyTextProperty);
        set => SetValue(ApplyTextProperty, value);
    }

    public ICommand? DiscardCommand
    {
        get => GetValue(DiscardCommandProperty);
        set => SetValue(DiscardCommandProperty, value);
    }

    public ICommand? ApplyCommand
    {
        get => GetValue(ApplyCommandProperty);
        set => SetValue(ApplyCommandProperty, value);
    }
}
