using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

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
        DirectoryTree = new ObservableCollection<BrowserDirectory>(Paths.GetTopLevelPreloadDirectories());

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

        if (!Paths.Exists(preloadDirectory)) return;

        var binFiles = Directory
            .EnumerateFiles(preloadDirectory, "*.bin", SearchOption.AllDirectories)
            .Where(f => f.Equals("MetaData.bin", StringComparison.OrdinalIgnoreCase) is not true);

        await Parallel.ForEachAsync(binFiles, async (binFile, cancellationToken) =>
        {
            var exported = await Serz.Convert(binFile, cancellationToken);
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
        var assetDirectories = Directory.GetDirectories(directory.FullPath);
        var preloadDirectory = assetDirectories.FirstOrDefault(d => d.Contains("PreLoad", StringComparison.OrdinalIgnoreCase));

        if (preloadDirectory is null)
        {
            throw new Exception($"Could not find preload directory {directory.FullPath}");
        }

        if (Paths.Exists(preloadDirectory))
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

        var blueprints = doc.QuerySelectorAll("Blueprint cConsistBlueprint").ToList();

        var consists = new List<PreloadConsist>();

        await Inner(blueprints);

        return consists;

        async Task Inner(List<IElement> elements)
        {
            foreach (var element in elements)
            {
                var parsed = PreloadConsist.Parse(element);

                if (parsed is null) continue;

                if (parsed.Blueprint.BlueprintId.Contains("fragment", StringComparison.OrdinalIgnoreCase))
                {
                    var fragmentDocument = await parsed.Blueprint.GetBlueprintXml();
                    var fragmentBlueprints = fragmentDocument.QuerySelectorAll("Blueprint cConsistFragmentBlueprint").ToList();

                    await Inner(fragmentBlueprints);
                }
                else
                {
                    consists.Add(parsed);
                }
            }
        }
    }
}
