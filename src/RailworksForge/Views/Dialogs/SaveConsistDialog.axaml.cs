using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

using RailworksForge.ViewModels;

using ReactiveUI;

namespace RailworksForge.Views.Dialogs;

public partial class SaveConsistDialog : ReactiveWindow<SaveConsistViewModel>
{
    public SaveConsistDialog()
    {
        InitializeComponent();

        if (Design.IsDesignMode) return;

        this.WhenActivated(action =>
        {
            var disposable = ViewModel!.SaveConsistCommand.Subscribe(Close);

            action(disposable);
        });
    }

    // ReSharper disable UnusedParameter.Local
    private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
