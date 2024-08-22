using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

using RailworksForge.ViewModels;

using ReactiveUI;

namespace RailworksForge.Views.Dialogs;

public partial class ReplaceTrackDialog : ReactiveWindow<ReplaceTrackViewModel>
{
    public ReplaceTrackDialog()
    {
        InitializeComponent();

        if (Design.IsDesignMode) return;

        this.WhenActivated(action =>
        {
            if (ViewModel is null) return;

            var disposable = ViewModel.ReplaceTracksCommand.Subscribe(Close);

            action(disposable);
        });
    }

    // ReSharper disable UnusedParameter.Local
    private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
