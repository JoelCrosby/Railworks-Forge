using Avalonia.Data.Converters;

namespace RailworksForge.Converters;

public static class ValueConverters
{
    public static FuncValueConverter<string?, bool> IsCompleteConverter { get; } = new (value =>
    {
        return value switch
        {
            "CompletedSuccessfully" => true,
            _ => false,
        };
    });
}
