using System;

using Avalonia.Controls;

namespace RailworksForge.Views.Components;

// A horizontal, scrollable list of a consist's vehicles in train order. Callers give it an item template, usually a
// VehicleCard.
public class ConsistStrip : ListBox
{
    private const string StyleClass = "consist-strip";

    // Styled as a ListBox to keep its template, so its own styles select this class rather than the type.
    public ConsistStrip()
    {
        Classes.Add(StyleClass);
    }

    protected override Type StyleKeyOverride => typeof(ListBox);
}
