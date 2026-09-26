using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;

using RailworksForge.Core;
using RailworksForge.Util;
using RailworksForge.ViewModels;

namespace RailworksForge.Views.Dialogs;

public partial class ReplaceConsistDialog : Window
{
    private readonly DataGridSortHandler _preloadConsistsDataGridSortHandler;

    public ReplaceConsistDialog()
    {
        InitializeComponent();

        _preloadConsistsDataGridSortHandler = new (PreloadConsistsDataGrid);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _preloadConsistsDataGridSortHandler.SortColumns();
    }

    private void ContextMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        if (DataContext is not ReplaceConsistViewModel { SelectedDirectory.AssetDirectory: ProviderDirectory })
        {
            return;
        }

        e.Cancel = true;
    }
}
