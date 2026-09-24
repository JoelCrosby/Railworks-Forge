using System.Linq;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.VisualTree;

namespace RailworksForge.Util;

public static class DataGridExtensions
{
    public static DataGridSortDescription? GetSort(this DataGridColumn column, DataGrid dataGrid)
    {
        var bindingPath = column.GetSortBindingPath();
        var sortDescriptions = dataGrid.CollectionView.SortDescriptions;
        var description =  sortDescriptions.FirstOrDefault(d => d.PropertyPath == bindingPath);

        return description;
    }


    public static bool IsFromDataGridRow(this RoutedEventArgs args)
    {
        var row = (args.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true);

        return row is not null;
    }

    public static string? GetSortBindingPath(this DataGridColumn column)
    {
        if (column is DataGridTemplateColumn templateColumn)
        {
            return templateColumn.SortMemberPath;
        }

        if (column is not DataGridBoundColumn boundColumn)
        {
            return null;
        }

        return boundColumn.Binding switch
        {
            Binding binding => binding.Path,
            CompiledBindingExtension compiledBinding => compiledBinding.Path.ToString(),
            _ => null,
        };
    }
}
