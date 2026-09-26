using System;

using Avalonia.Controls;

using RailworksForge.ViewModels;

namespace RailworksForge.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Start();
        }
    }
}
