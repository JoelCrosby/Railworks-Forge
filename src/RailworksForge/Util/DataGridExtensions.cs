using Avalonia.Controls;
using Avalonia.Data;

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
            ReflectionBinding binding => binding.Path,
            CompiledBinding compiledBinding => compiledBinding.Path?.ToString(),
            _ => null,
        };
    }
}
