using System;

using Avalonia;
using Avalonia.Media;

using LucideAvalonia;

namespace RailworksForge.Views.Controls;

// Lucide's drawings measure to their visible bounds, so each glyph stretches to fill the control and loses the
// padding Lucide builds into its 24×24 canvas. Pinning that canvas renders icons at the size the designs use.
public class LucideIcon : Lucide
{
    private static readonly Rect LucideCanvas = new(0, 0, 24, 24);

    // Lucide's XAML sets a local 24×24 size, which would outrank the sizes our styles give each icon.
    public LucideIcon()
    {
        ClearValue(WidthProperty);
        ClearValue(HeightProperty);
    }

    protected override Type StyleKeyOverride => typeof(Lucide);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != IconSourceProperty)
        {
            return;
        }

        if (IconSource is DrawingImage { Viewbox: null, Drawing: {} drawing })
        {
            IconSource = new DrawingImage(drawing) { Viewbox = LucideCanvas };
        }
    }
}
