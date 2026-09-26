using System.Collections;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace RailworksForge.Behaviours;

// DataGrid.SelectedItems is not bindable, so mirror it into a view model collection.
public class DataGridSelectedItemsBehaviour : StyledElementBehavior<DataGrid>
{
    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<DataGridSelectedItemsBehaviour, IList?>(nameof(SelectedItems));

    public IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            AssociatedObject.SelectionChanged += OnSelectionChanged;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
        }

        base.OnDetaching();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject is null || SelectedItems is null)
        {
            return;
        }

        SelectedItems.Clear();

        foreach (var item in AssociatedObject.SelectedItems)
        {
            SelectedItems.Add(item);
        }
    }
}
