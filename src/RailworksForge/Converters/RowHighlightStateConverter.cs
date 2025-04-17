using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Converters;

public class RowHighlightStateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConsistViewModel { Consist: {} consist })
        {
            if (consist.ConsistAcquisitionState is AcquisitionState.Missing)
            {
                return Brushes.DarkRed;
            }

            if (consist.ConsistAcquisitionState is AcquisitionState.Partial)
            {
                return Brushes.DarkGoldenrod;
            }

            if (consist.PlayerDriver)
            {
                return Brushes.DarkBlue;
            }
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
