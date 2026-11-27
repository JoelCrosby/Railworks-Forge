using System.Windows.Input;

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace RailworksForge.Views.Components;

public class SelectionToolbar : TemplatedControl
{
    public static readonly StyledProperty<int> SelectedCountProperty =
        AvaloniaProperty.Register<SelectionToolbar, int>(nameof(SelectedCount));

    public static readonly StyledProperty<string?> SelectionTextProperty =
        AvaloniaProperty.Register<SelectionToolbar, string?>(nameof(SelectionText));

    public static readonly StyledProperty<string?> ClearTextProperty =
        AvaloniaProperty.Register<SelectionToolbar, string?>(nameof(ClearText));

    public static readonly StyledProperty<ICommand?> ClearCommandProperty =
        AvaloniaProperty.Register<SelectionToolbar, ICommand?>(nameof(ClearCommand));

    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<SelectionToolbar, object?>(nameof(Actions));

    static SelectionToolbar()
    {
        IsVisibleProperty.OverrideDefaultValue<SelectionToolbar>(false);
    }

    public int SelectedCount
    {
        get => GetValue(SelectedCountProperty);
        set => SetValue(SelectedCountProperty, value);
    }

    public string? SelectionText
    {
        get => GetValue(SelectionTextProperty);
        set => SetValue(SelectionTextProperty, value);
    }

    public string? ClearText
    {
        get => GetValue(ClearTextProperty);
        set => SetValue(ClearTextProperty, value);
    }

    public ICommand? ClearCommand
    {
        get => GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    [Content]
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedCountProperty)
        {
            IsVisible = SelectedCount > 0;
        }
    }
}
