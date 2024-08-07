using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, PreloadConsist?> ReplaceConsistCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadAvailableStockCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }

    public required Scenario Scenario { get; init; }

    [ObservableProperty]
    private ObservableCollection<PreloadConsistViewModel> _availableStock;

    [ObservableProperty]
    private PreloadConsistViewModel? _selectedConsist;

    [ObservableProperty]
    private BrowserDirectory? _selectedDirectory;

    public ObservableCollection<BrowserDirectory> DirectoryTree { get; }

    public ReplaceConsistViewModel()
    {
        AvailableStock = [];
        DirectoryTree = new ObservableCollection<BrowserDirectory>(Paths.GetTopLevelRailVehicleDirectories());

        ReplaceConsistCommand = ReactiveCommand.Create(() => SelectedConsist?.Consist);
        LoadAvailableStockCommand = ReactiveCommand.CreateFromTask(LoadAvailableStock);
        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedDirectory is null) return;

            Launcher.Open(SelectedDirectory.FullPath);
        });
    }

    private async Task LoadAvailableStock()
    {
        Dispatcher.UIThread.Post(AvailableStock.Clear);

        if (SelectedDirectory is null) return;

        var preloadDirectory = GetPreloadDirectory(SelectedDirectory);

        if (!Directory.Exists(preloadDirectory)) return;

        var binFiles = Directory
            .EnumerateFiles(preloadDirectory, "*.bin", SearchOption.AllDirectories)
            .Where(f => f.Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase) is not true);

        await Parallel.ForEachAsync(binFiles, async (binFile, cancellationToken) =>
        {
            var exported = await Serz.Convert(binFile, false, cancellationToken);
            var consists = await GetConsistBlueprints(exported.OutputPath, cancellationToken);
            var models = consists.ConvertAll(c => new PreloadConsistViewModel(c));

            LoadImages(models);

            Dispatcher.UIThread.Post(() => AvailableStock.AddRange(models));
        });
    }

    private static async void LoadImages(IEnumerable<PreloadConsistViewModel> items)
    {
        foreach (var item in items)
        {
            await item.LoadImage();
        }
    }

    private static string GetPreloadDirectory(BrowserDirectory directory)
    {
        var preloadDirectory = Path.Join(directory.FullPath, "PreLoad");

        if (Directory.Exists(preloadDirectory))
        {
            return preloadDirectory;
        }

        foreach (var package in Directory.EnumerateFiles(directory.FullPath, "*.ap"))
        {
            Archives.ExtractDirectory(package, "PreLoad");
        }

        return preloadDirectory;
    }

    private static async Task<List<PreloadConsist>> GetConsistBlueprints(string path, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        var doc = await XmlParser.ParseDocumentAsync(text, cancellationToken);

        return doc
            .QuerySelectorAll("Blueprint cConsistBlueprint")
            .Select(PreloadConsist.Parse)
            .ToList();
    }
}
