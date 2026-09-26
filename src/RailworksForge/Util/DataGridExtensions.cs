using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace RailworksForge.Util;

public static class DataGridExtensions
{
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
            CompiledBindingExtension compiledBinding => compiledBinding.Path?.ToString(),
            _ => null,
        };
    }
}
