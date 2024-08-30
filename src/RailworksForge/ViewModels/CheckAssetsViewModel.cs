using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class CheckAssetsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Route _route;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _loadingProgress;

    [ObservableProperty]
    private string _loadingMessage;

    [ObservableProperty]
    private ObservableCollection<Blueprint> _blueprints;

    public CheckAssetsViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        Blueprints = [];

        Observable.Start(GetMissingAssets, RxApp.TaskpoolScheduler);
    }

    private async Task GetMissingAssets()
    {
        var sceneryPath = Path.Join(Route.DirectoryPath, "Scenery");
        var binFiles = Directory.EnumerateFiles(sceneryPath, "*.bin").ToList();

        var results = new HashSet<Blueprint>();

        var processedCount = 0;
        var processed = binFiles.Count;

        await Parallel.ForEachAsync(binFiles, async (path, cancellationToken) =>
        {
            var serialised = await Serz.Convert(path, true, cancellationToken);
            var xml  = await File.ReadAllTextAsync(serialised.OutputPath, cancellationToken);
            var document = await XmlParser.ParseDocumentAsync(xml, cancellationToken);

            var entities = document.QuerySelectorAll("cDynamicEntity BlueprintID");

            var blueprints = entities.Select(el => new Blueprint
            {
                BlueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Provider"),
                BlueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Product"),
                BlueprintId = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID"),
            });

            foreach (var blueprint in blueprints)
            {
                results.Add(blueprint);
            }

            processedCount++;

            LoadingProgress = (int) Math.Ceiling((double)(100 * processedCount) / processed);
            LoadingMessage = $"Processed {processedCount} of {processed} files ( %{LoadingProgress} )";
        });

        Dispatcher.UIThread.Post(() =>
        {
            Blueprints.AddRange(results);
            IsLoading = false;
        });
    }
}
