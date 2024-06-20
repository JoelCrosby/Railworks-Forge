using System.Reactive.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using RailworksForge.ViewModels;

using ReactiveUI;

namespace RailworksForge.Views.Controls;

public partial class FileBrowser : UserControl
{
    public FileBrowser()
    {
        InitializeComponent();
    }

    private void LoadAvailableStockOnClick(object? _sender, RoutedEventArgs _e)
    {
        if (DataContext is not FileBrowserViewModel viewModel) return;

        var parent = this.FindAncestorOfType<ReplaceConsistDialog>();

        if (parent is not { DataContext: ReplaceConsistViewModel model }) return;
        if (viewModel.SelectedItem is not { } selected) return;

        Observable.Start(() => model.LoadAvailableStock(selected), RxApp.TaskpoolScheduler);
    }
}
