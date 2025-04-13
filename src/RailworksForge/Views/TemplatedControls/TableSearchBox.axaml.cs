using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace RailworksForge.Views.TemplatedControls;

public class TableSearchBox : TemplatedControl
{
    public static readonly StyledProperty<string> SearchTermProperty =
        AvaloniaProperty.Register<TableSearchBox, string>(nameof(SearchTerm), defaultBindingMode: BindingMode.TwoWay);

    public string SearchTerm
    {
        get => GetValue(SearchTermProperty);
        set => SetValue(SearchTermProperty, value);
    }
}
