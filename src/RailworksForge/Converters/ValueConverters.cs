using System;

using Avalonia.Data.Converters;

namespace RailworksForge.Converters;

public static class ValueConverters
{
    private const int MaximumRating = 5;

    public static FuncValueConverter<string?, bool> IsCompleteConverter { get; } = new (value =>
    {
        return value switch
        {
            "CompletedSuccessfully" => true,
            _ => false,
        };
    });

    public static FuncValueConverter<int, string> DurationConverter { get; } = new (minutes =>
    {
        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;

        return (hours, remainingMinutes) switch
        {
            (0, 0) => "–",
            (0, _) => $"{remainingMinutes}m",
            (_, 0) => $"{hours}h",
            _ => $"{hours}h {remainingMinutes:00}m",
        };
    });

    public static FuncValueConverter<int, string> RatingConverter { get; } = new (rating =>
    {
        var filled = Math.Clamp(rating, 0, MaximumRating);
        var empty = MaximumRating - filled;

        return new string('●', filled) + new string('○', empty);
    });
}
