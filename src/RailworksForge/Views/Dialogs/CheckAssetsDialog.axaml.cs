using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

using RailworksForge.ViewModels;

namespace RailworksForge.Views.Dialogs;

public partial class CheckAssetsDialog : ReactiveWindow<CheckAssetsViewModel>
{
    public CheckAssetsDialog()
    {
        InitializeComponent();
    }

    // ReSharper disable UnusedParameter.Local
    private void CloseButtonOnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CheckAssetsViewModel model)
        {
            model.OnClose();
        }

        Close();
    }
}
