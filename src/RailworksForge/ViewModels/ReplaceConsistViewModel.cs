using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using AngleSharp.Html.Parser;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, PreloadConsist?> ReplaceConsistCommand { get; }

    [ObservableProperty]
    private ObservableCollection<PreloadConsistViewModel> _availableStock;

    public required Consist TargetConsist { get; init; }

    [ObservableProperty]
    private FileBrowserViewModel _fileBrowser;

    [ObservableProperty]
    private PreloadConsist? _selectedConsist;

    public ReplaceConsistViewModel()
    {
        AvailableStock = [];
        ReplaceConsistCommand = ReactiveCommand.Create(() => SelectedConsist);
        FileBrowser = new FileBrowserViewModel(Paths.GetAssetsDirectory());
    }

    public async Task LoadAvailableStock(BrowserDirectory directory)
    {
        Dispatcher.UIThread.Post(AvailableStock.Clear);

        var preloadDirectory = GetPreloadDirectory(directory);

        if (!Directory.Exists(preloadDirectory)) return;

        var binFiles = Directory
            .EnumerateFiles(preloadDirectory, "*.bin", SearchOption.AllDirectories)
            .Where(f => f.Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase) is not true);

        await Parallel.ForEachAsync(binFiles, async (binFile, cancellationToken) =>
        {
            var exported = await Serz.Convert(binFile, cancellationToken);
            var consists = await GetConsistBlueprints(exported.OutputPath);
            var models = consists.ConvertAll(c => new PreloadConsistViewModel(c));

            LoadImages(models);

            Dispatcher.UIThread.Post(() => AvailableStock.AddRange(models));
        });
    }

    private static async void LoadImages(IEnumerable<PreloadConsistViewModel> items)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8,
        };

        await Parallel.ForEachAsync(items, options, async (route, _) => await route.LoadImage());
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

    private static async Task<List<PreloadConsist>> GetConsistBlueprints(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        var doc = await new HtmlParser().ParseDocumentAsync(text);

        return doc
            .QuerySelectorAll("Blueprint cConsistBlueprint")
            .Select(PreloadConsist.Parse)
            .ToList();
    }
}
