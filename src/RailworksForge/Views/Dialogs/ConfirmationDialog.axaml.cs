using System;

using Avalonia.Controls;
using Avalonia.ReactiveUI;

using RailworksForge.ViewModels;

using ReactiveUI;


namespace RailworksForge.Views.Dialogs;

public partial class ConfirmationDialog : ReactiveWindow<ConfirmationDialogViewModel>
{
    public ConfirmationDialog()
    {
        InitializeComponent();

        if (Design.IsDesignMode) return;

        this.WhenActivated(action =>
        {
            if (ViewModel is null) return;

            var accept = ViewModel.AcceptClickedCommand.Subscribe(result => { Close(result); });
            var cancel = ViewModel.CancelClickedCommand.Subscribe(result => { Close(result); });

            action(accept);
            action(cancel);
        });
    }
}
