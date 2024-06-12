using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace RailworksForge.Converters;

public class IdValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text;
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
