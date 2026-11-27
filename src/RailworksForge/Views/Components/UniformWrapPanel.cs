using System;

using Avalonia;
using Avalonia.Controls;

namespace RailworksForge.Views.Components;

public class UniformWrapPanel : Panel
{
    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<UniformWrapPanel, double>(nameof(MinItemWidth), 240);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<UniformWrapPanel, double>(nameof(ColumnSpacing));

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<UniformWrapPanel, double>(nameof(RowSpacing));

    static UniformWrapPanel()
    {
        AffectsMeasure<UniformWrapPanel>(MinItemWidthProperty, ColumnSpacingProperty, RowSpacingProperty);
    }

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? FallbackWidth() : availableSize.Width;
        var columns = GetColumnCount(width);
        var itemWidth = GetItemWidth(width, columns);
        var height = 0.0;
        var rowHeight = 0.0;

        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];

            child.Measure(new Size(itemWidth, double.PositiveInfinity));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);

            var isRowEnd = (index + 1) % columns == 0 || index == Children.Count - 1;

            if (isRowEnd)
            {
                var isFirstRow = index < columns;

                height += rowHeight + (isFirstRow ? 0 : RowSpacing);
                rowHeight = 0;
            }
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = GetColumnCount(finalSize.Width);
        var itemWidth = GetItemWidth(finalSize.Width, columns);
        var y = 0.0;

        for (var rowStart = 0; rowStart < Children.Count; rowStart += columns)
        {
            var rowEnd = Math.Min(rowStart + columns, Children.Count);
            var rowHeight = 0.0;

            for (var index = rowStart; index < rowEnd; index++)
            {
                rowHeight = Math.Max(rowHeight, Children[index].DesiredSize.Height);
            }

            for (var index = rowStart; index < rowEnd; index++)
            {
                var x = (index - rowStart) * (itemWidth + ColumnSpacing);

                Children[index].Arrange(new Rect(x, y, itemWidth, rowHeight));
            }

            y += rowHeight + RowSpacing;
        }

        return finalSize;
    }

    private double FallbackWidth()
    {
        var count = Math.Max(Children.Count, 1);

        return count * MinItemWidth + (count - 1) * ColumnSpacing;
    }

    private int GetColumnCount(double width)
    {
        var fitting = (int)Math.Floor((width + ColumnSpacing) / (MinItemWidth + ColumnSpacing));

        return Math.Max(fitting, 1);
    }

    private double GetItemWidth(double width, int columns)
    {
        var spacing = ColumnSpacing * (columns - 1);

        return Math.Max((width - spacing) / columns, 0);
    }
}
