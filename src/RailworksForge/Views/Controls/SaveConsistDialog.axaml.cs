using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

using RailworksForge.ViewModels;

using ReactiveUI;

namespace RailworksForge.Views.Controls;

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

    private void CancelButtonOnClick(object? _sender, RoutedEventArgs _e)
    {
        Close();
    }
}
