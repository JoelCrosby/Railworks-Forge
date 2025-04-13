using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

using RailworksForge.Core;
using RailworksForge.Util;
using RailworksForge.ViewModels;

using ReactiveUI;

namespace RailworksForge.Views.Dialogs;

public partial class ReplaceConsistDialog : ReactiveWindow<ReplaceConsistViewModel>
{
    private readonly TreeDataGridSortHandler _preloadConsistsDataGridSortHandler;

    public ReplaceConsistDialog()
    {
        InitializeComponent();

        _preloadConsistsDataGridSortHandler  = new (PreloadConsistsDataGrid);

        if (Design.IsDesignMode) return;

        this.WhenActivated(action =>
        {
            if (ViewModel is null) return;

            var disposable = ViewModel.ReplaceConsistCommand.Subscribe(Close);

            action(disposable);
        });
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _preloadConsistsDataGridSortHandler.SortColumns();
    }

    // ReSharper disable UnusedParameter.Local
    private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ContextMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        if (ViewModel is not {} model) return;

        if (model.SelectedDirectory?.AssetDirectory is ProviderDirectory)
        {
            e.Cancel = true;
        }
    }
}
