using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using RailworksForge.Core.Config;

namespace RailworksForge.Util;

public class TreeDataGridSortHandler(TreeDataGrid dataGrid)
{
    private string DataGridName => dataGrid.Name ?? throw new Exception("DataGridUserControl requires a named data grid");

    public void SortColumns()
    {
        InitialiseSortBehaviour();

        var options = Configuration.Get().DataGrids.GetValueOrDefault(DataGridName);

        if (options is not { SortingColumn: { } header, SortingDirection: { } sortingDirection })
        {
            return;
        }

        var source = dataGrid.Source;

        if (source is null) return;

        var direction = sortingDirection == "asc" ? ListSortDirection.Ascending : ListSortDirection.Descending;
        var column = source.Columns.FirstOrDefault(c => c.Header?.ToString() == header);

        if (column is null) return;

        source.SortBy(column, direction);
    }

    private void InitialiseSortBehaviour()
    {
        if (dataGrid.Source is null) return;

        dataGrid.Source.Sorted += () => _ = DataGrid_OnSorting();
    }

    private async Task DataGrid_OnSorting()
    {
        await Task.Delay(200).ConfigureAwait(false);

        var source = dataGrid.Source;

        if (source is null) return;

        var config = Configuration.Get();
        var column = source.Columns.FirstOrDefault(c => c.SortDirection != null);

        if (column?.SortDirection is null) return;

        var direction = column.SortDirection;
        var sortingColumn = column.Header!.ToString();

        if (sortingColumn is null) return;

        config.DataGrids[DataGridName] = new DataGridOptions
        {
            SortingColumn = sortingColumn,
            SortingDirection = direction == ListSortDirection.Descending ? "desc" : "asc",
        };

        Configuration.Set(config);
    }
}
