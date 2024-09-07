using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace RailworksForge.Controls;

public class DataTableControl : TemplatedControl
{
    public static readonly StyledProperty<ITreeDataGridSource> DataSourceProperty = AvaloniaProperty
        .Register<DataTableControl, ITreeDataGridSource>(nameof(DataSource));

    public ITreeDataGridSource DataSource
    {
        get => GetValue(DataSourceProperty);
        set => SetValue(DataSourceProperty, value);
    }

    public static readonly StyledProperty<string> SearchTermProperty = AvaloniaProperty
        .Register<DataTableControl, string>(nameof(SearchTerm), "Search");

    public string SearchTerm
    {
        get => GetValue(SearchTermProperty);
        set
        {
            if (DataSource is FlatTreeDataGridSource<object> dataSource)
            {
                // dataSource.?
            }

            SetValue(SearchTermProperty, value);
        }
    }

    public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent =
    RoutedEvent.Register<DataTableControl, RoutedEventArgs>(nameof(ValueChanged), RoutingStrategies.Direct);

    public event EventHandler<RoutedEventArgs> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected virtual void OnValueChanged()
    {
        RoutedEventArgs args = new RoutedEventArgs(ValueChangedEvent);
        RaiseEvent(args);


    }
}
