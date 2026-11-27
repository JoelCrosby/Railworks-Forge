using System;
using System.Collections;

using Avalonia.Controls;

namespace RailworksForge.Behaviours;

public class DataGridSelectedItemsBehaviour : SelectedItemsBehaviour<DataGrid>
{
    protected override IList GetControlSelection(DataGrid control)
    {
        return control.SelectedItems;
    }

    protected override void SubscribeToSelection(DataGrid control, EventHandler<SelectionChangedEventArgs> handler)
    {
        control.SelectionChanged += handler;
    }

    protected override void UnsubscribeFromSelection(DataGrid control, EventHandler<SelectionChangedEventArgs> handler)
    {
        control.SelectionChanged -= handler;
    }
}
