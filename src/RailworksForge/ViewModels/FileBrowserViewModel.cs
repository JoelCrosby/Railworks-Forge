using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class FileBrowserViewModel : ViewModelBase
{
    public IObservable<ObservableCollection<BrowserDirectory>> Items { get; }

    public string Path { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }

    [ObservableProperty]
    private BrowserDirectory? _selectedItem;

    public FileBrowserViewModel(string path)
    {
        Path = path;

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem is null) return;

            Launcher.Open(SelectedItem.FullPath);
        });

        Items = Observable.Start(Load, RxApp.TaskpoolScheduler);
    }

    private static ObservableCollection<BrowserDirectory> Load()
    {
        var items = Paths.GetTopLevelRailVehicleDirectories();
        return new ObservableCollection<BrowserDirectory>(items);
    }
}
