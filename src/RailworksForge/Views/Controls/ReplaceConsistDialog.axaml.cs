using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

using RailworksForge.ViewModels;

using ReactiveUI;

namespace RailworksForge.Views.Controls;

public partial class ReplaceConsistDialog : ReactiveWindow<ReplaceConsistViewModel>
{
    public ReplaceConsistDialog()
    {
        InitializeComponent();

        if (Design.IsDesignMode) return;

        this.WhenActivated(action =>
        {
            if (ViewModel is null) return;

            var disposable = ViewModel.ReplaceConsistCommand.Subscribe(_ => Close());

            action(disposable);
        });
    }

    // ReSharper disable UnusedParameter.Local
    private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
