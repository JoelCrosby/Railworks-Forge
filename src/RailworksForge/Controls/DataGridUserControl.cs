using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Threading;

using RailworksForge.Core.Config;
using RailworksForge.Util;

namespace RailworksForge.Controls;

public abstract class DataGridUserControl : UserControl
{
    protected abstract DataGrid DataGrid { get; }

    private string DataGridName => DataGrid.Name ?? throw new Exception("DataGridUserControl requires a named data grid");

    protected void SortColumns()
    {
        var options = Configuration.Get().DataGrids.GetValueOrDefault(DataGridName);

        if (options is not { SortingColumn: { } header, SortingDirection: { } sortingDirection })
        {
            return;
        }

        var direction = sortingDirection == "asc" ? ListSortDirection.Ascending : ListSortDirection.Descending;
        var column = DataGrid.Columns.FirstOrDefault(c => c.GetSortBindingPath() == header);

        Dispatcher.UIThread.InvokeAsync(() => column?.Sort(direction));
    }

    protected void DataGrid_OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;

        var config = Configuration.Get();
        var direction = e.Column.GetSort(dataGrid);
        var sortingColumn = e.Column.GetSortBindingPath();

        if (sortingColumn is null) return;

        config.DataGrids[DataGridName] = new DataGridOptions
        {
            SortingColumn = sortingColumn,
            SortingDirection = direction?.Direction == ListSortDirection.Ascending ? "desc" : "asc",
        };

        Configuration.Set(config);
    }
}
