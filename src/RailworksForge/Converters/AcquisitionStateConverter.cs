using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.Converters;

public class AcquisitionStateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ConsistViewModel { Consist.ConsistAcquisitionState : AcquisitionState.Missing } ? Brushes.DarkRed : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
