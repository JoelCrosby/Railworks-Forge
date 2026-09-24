using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;

using RailworksForge.Core.Config;

namespace RailworksForge.Util;

public class DataGridSortHandler
{
    private readonly DataGrid _dataGrid;

    private string DataGridName => _dataGrid.Name ?? throw new Exception("DataGridSortHandler requires a named data grid");

    public DataGridSortHandler(DataGrid dataGrid)
    {
        _dataGrid = dataGrid;
        _dataGrid.Sorting += DataGrid_OnSorting;
    }

    public void SortColumns()
    {
        var options = Configuration.Get().DataGrids.GetValueOrDefault(DataGridName);

        if (options is not { SortingColumn: { } sortingColumn, SortingDirection: { } sortingDirection })
        {
            return;
        }

        var sortDescriptions = _dataGrid.CollectionView?.SortDescriptions;

        if (sortDescriptions is null)
        {
            return;
        }

        var hasMatchingColumn = _dataGrid.Columns.Any(c => c.GetSortBindingPath() == sortingColumn);

        if (!hasMatchingColumn)
        {
            return;
        }

        var direction = sortingDirection == "asc" ? ListSortDirection.Ascending : ListSortDirection.Descending;
        var sortDescription = DataGridSortDescription.FromPath(sortingColumn, direction);

        sortDescriptions.Clear();
        sortDescriptions.Add(sortDescription);
    }

    // Sorting is raised before the grid updates its sort descriptions, so read them once it has.
    private void DataGrid_OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        Dispatcher.UIThread.Post(SaveSort);
    }

    private void SaveSort()
    {
        var sortDescription = _dataGrid.CollectionView?.SortDescriptions.FirstOrDefault();

        if (sortDescription is not { HasPropertyPath: true, PropertyPath: { } sortingColumn })
        {
            return;
        }

        var config = Configuration.Get();
        var isDescending = sortDescription.Direction == ListSortDirection.Descending;

        config.DataGrids[DataGridName] = new DataGridOptions
        {
            SortingColumn = sortingColumn,
            SortingDirection = isDescending ? "desc" : "asc",
        };

        Configuration.Set(config);
    }
}
