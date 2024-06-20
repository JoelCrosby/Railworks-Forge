using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class ReplaceConsistViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, SavedConsist?> ReplaceConsistCommand { get; }

    [ObservableProperty]
    private ObservableCollection<ConsistBlueprint> _availableStock;

    public required Consist TargetConsist { get; init; }

    [ObservableProperty]
    private FileBrowserViewModel _fileBrowser;

    [ObservableProperty]
    private SavedConsist? _selectedConsist;

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

            Dispatcher.UIThread.Post(() => AvailableStock.AddRange(consists));
        });
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

    private static async Task<List<ConsistBlueprint>> GetConsistBlueprints(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        var doc = await new HtmlParser().ParseDocumentAsync(text);

        return doc
            .QuerySelectorAll("Blueprint cConsistBlueprint")
            .Select(ParseConsistBlueprint)
            .ToList();
    }

    private static ConsistBlueprint ParseConsistBlueprint(IElement el)
    {
        var locomotiveName = el.SelectTextContnet("LocoName English");
        var displayName = el.SelectTextContnet("DisplayName English");

        return new ConsistBlueprint
        {
            LocomotiveName = locomotiveName,
            DisplayName = displayName,
        };
    }
}