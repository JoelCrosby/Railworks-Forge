using Avalonia;
using Avalonia.Controls.Primitives;

namespace RailworksForge.Views.TemplatedControls;

public class BlueprintSummary : TemplatedControl
{
    public static readonly StyledProperty<string?> ProviderProperty =
        AvaloniaProperty.Register<BlueprintSummary, string?>(nameof(Provider));

    public static readonly StyledProperty<string?> ProductProperty =
        AvaloniaProperty.Register<BlueprintSummary, string?>(nameof(Product));

    public static readonly StyledProperty<string?> BlueprintIdProperty =
        AvaloniaProperty.Register<BlueprintSummary, string?>(nameof(BlueprintId));

    public string? Provider
    {
        get => GetValue(ProviderProperty);
        set => SetValue(ProviderProperty, value);
    }

    public string? Product
    {
        get => GetValue(ProductProperty);
        set => SetValue(ProductProperty, value);
    }

    public string? BlueprintId
    {
        get => GetValue(BlueprintIdProperty);
        set => SetValue(BlueprintIdProperty, value);
    }
}
