using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using RailworksForge.Core.Config;

namespace RailworksForge.Controls;

public abstract class TreeDataGridUserControl : UserControl
{
    protected abstract TreeDataGrid DataGrid { get; }

    private string DataGridName => DataGrid.Name ?? throw new Exception("DataGridUserControl requires a named data grid");

    protected void SortColumns()
    {
        InitialiseSortBehaviour();

        var options = Configuration.Get().DataGrids.GetValueOrDefault(DataGridName);

        if (options is not { SortingColumn: { } header, SortingDirection: { } sortingDirection })
        {
            return;
        }

        var source = DataGrid.Source;

        if (source is null) return;

        var direction = sortingDirection == "asc" ? ListSortDirection.Ascending : ListSortDirection.Descending;
        var column = source.Columns.FirstOrDefault(c => c.Header?.ToString() == header);

        if (column is null) return;

        source.SortBy(column, direction);
    }

    private void InitialiseSortBehaviour()
    {
        if (DataGrid.Source is null) return;

        DataGrid.Source.Sorted += () => _ = DataGrid_OnSorting();
    }

    private async Task DataGrid_OnSorting()
    {
        await Task.Delay(200).ConfigureAwait(false);

        var source = DataGrid.Source;

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
