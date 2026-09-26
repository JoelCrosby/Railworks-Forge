using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace RailworksForge.Behaviours;

// Runs the command only when a row or list item is double tapped, not a header or empty space.
public class ItemDoubleTapCommandBehaviour : StyledElementBehavior<Control>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<ItemDoubleTapCommandBehaviour, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            AssociatedObject.DoubleTapped += OnDoubleTapped;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.DoubleTapped -= OnDoubleTapped;
        }

        base.OnDetaching();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var source = e.Source as Visual;
        var isFromRow = source?.FindAncestorOfType<DataGridRow>(includeSelf: true) is not null;
        var isFromListItem = source?.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not null;
        var isFromItem = isFromRow || isFromListItem;
        var canExecute = Command?.CanExecute(null) is true;
        var shouldExecute = isFromItem && canExecute;

        if (shouldExecute)
        {
            Command!.Execute(null);
        }
    }
}
