using Avalonia;
using Avalonia.Controls;

using RailworksForge.ViewModels;

namespace RailworksForge.Util;

public class DataGridRowHighlight : AvaloniaObject
{
    public static readonly AttachedProperty<RowHighlightState> StateProperty =
        AvaloniaProperty.RegisterAttached<DataGridRowHighlight, DataGridRow, RowHighlightState>("State");

    public static RowHighlightState GetState(DataGridRow row)
    {
        return row.GetValue(StateProperty);
    }

    public static void SetState(DataGridRow row, RowHighlightState value)
    {
        row.SetValue(StateProperty, value);
    }
}
