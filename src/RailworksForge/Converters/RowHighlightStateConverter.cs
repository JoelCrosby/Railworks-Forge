using System;
using System.Globalization;

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Converters;

public class RowHighlightStateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Application.Current!;
        var theme = app.ActualThemeVariant;
        var resources = app.Resources;

        if (value is ConsistViewModel { Consist: {} consist })
        {
            if (consist.ConsistAcquisitionState is AcquisitionState.Missing)
            {
                if (resources.TryGetResource("ErrorBrush", theme, out var res))
                {
                    return res;
                }
            }

            if (consist.ConsistAcquisitionState is AcquisitionState.Partial)
            {
                if (resources.TryGetResource("PartialBrush", theme, out var res))
                {
                    return res;
                }
            }

            if (consist.PlayerDriver)
            {
                if (resources.TryGetResource("IsPlayerBrush", theme, out var res))
                {
                    return res;
                }
            }
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
