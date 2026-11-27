using System;
using System.Collections;

using Avalonia.Controls;

namespace RailworksForge.Behaviours;

public class ListBoxSelectedItemsBehaviour : SelectedItemsBehaviour<ListBox>
{
    protected override IList GetControlSelection(ListBox control)
    {
        return control.SelectedItems!;
    }

    protected override void SubscribeToSelection(ListBox control, EventHandler<SelectionChangedEventArgs> handler)
    {
        control.SelectionChanged += handler;
    }

    protected override void UnsubscribeFromSelection(ListBox control, EventHandler<SelectionChangedEventArgs> handler)
    {
        control.SelectionChanged -= handler;
    }
}
