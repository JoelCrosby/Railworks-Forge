using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class FileBrowserViewModel : ViewModelBase
{
    public IObservable<ObservableCollection<BrowserDirectory>> Items { get; }

    public string Path { get; }

    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }

    [ObservableProperty]
    public BrowserDirectory? _selectedItem;

    public FileBrowserViewModel(string path)
    {
        Path = path;

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem is null) return;

            Launcher.Open(SelectedItem.Path);
        });

        Items = Observable.Start(GetSubfoldersAsync, RxApp.TaskpoolScheduler);
    }

    private ObservableCollection<BrowserDirectory> GetSubfoldersAsync()
    {
        return GetSubfolders(Path);
    }

    private static ObservableCollection<BrowserDirectory> GetSubfolders(string path)
    {
        var directories = new List<BrowserDirectory>();
        var subDirectories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

        foreach (var subDirectory in subDirectories)
        {
            var dir = new BrowserDirectory(subDirectory);

            if (Directory.GetDirectories(subDirectory, "*", SearchOption.TopDirectoryOnly).Length > 0)
            {
                dir.Subfolders = GetSubfolders(subDirectory);
            }

            directories.Add(dir);
        }

        var sorted = directories.OrderBy(dir => !dir.HasChildren).ThenBy(dir => dir.Name);

        return new ObservableCollection<BrowserDirectory>(sorted);
    }

    public class BrowserDirectory
    {
        public ObservableCollection<BrowserDirectory> Subfolders { get; set; }

        public string Name { get; }
        public string Path { get; }
        public bool HasChildren => Subfolders.Count > 0;

        public BrowserDirectory(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
            Subfolders = [];
        }
    }
}
