using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

using RailworksForge.Core;
using RailworksForge.ViewModels;

using ReactiveUI;

namespace RailworksForge.Views.Dialogs;

public partial class ReplaceConsistDialog : ReactiveWindow<ReplaceConsistViewModel>
{
    public ReplaceConsistDialog()
    {
        InitializeComponent();

        if (Design.IsDesignMode) return;

        this.WhenActivated(action =>
        {
            if (ViewModel is null) return;

            var disposable = ViewModel.ReplaceConsistCommand.Subscribe(Close);

            action(disposable);
        });
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
