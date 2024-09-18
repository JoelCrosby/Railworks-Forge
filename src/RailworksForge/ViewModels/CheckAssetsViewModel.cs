using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Core.Extensions;
using RailworksForge.Core.External;
using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

using ReactiveUI;

using Serilog;

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

    private readonly CancellationTokenSource _cts = new ();

    public CheckAssetsViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        Blueprints = [];
        LoadingMessage = string.Empty;

        if (Design.IsDesignMode)
        {
            return;
        }

        Observable.Start(GetMissingAssets, RxApp.TaskpoolScheduler);
    }

    private async Task GetMissingAssets()
    {
        var binFiles = GetBinFiles();

        var results = new HashSet<Blueprint>();

        var processedCount = 0;
        var processed = binFiles.Count;

        await Parallel.ForEachAsync(binFiles, _cts.Token, async (path, _) =>
        {
            try
            {
                var serialised = await Serz.Convert(path);
                var xml  = File.ReadAllText(serialised.OutputPath);

                using var document = XmlParser.ParseDocument(xml);

                var entities = document.QuerySelectorAll("cDynamicEntity BlueprintID");

                var blueprints = entities.Select(el => new Blueprint
                {
                    BlueprintSetIdProvider = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Provider"),
                    BlueprintSetIdProduct = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID Product"),
                    BlueprintId = el.SelectTextContent("iBlueprintLibrary-cAbsoluteBlueprintID BlueprintID"),
                });

                foreach (var blueprint in blueprints)
                {
                    if (string.IsNullOrWhiteSpace(blueprint.BlueprintId))
                    {
                        continue;
                    }

                    results.Add(blueprint);
                }

                processedCount++;

                var count = processedCount;

                Dispatcher.UIThread.Post(() =>
                {
                    LoadingProgress = (int) Math.Ceiling((double)(100 * count) / processed);
                    LoadingMessage = $"Processed {count} of {processed} files ( %{LoadingProgress} )";
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "check assets for path path failed");
            }
        });

        var missing = await Task.Run(() => results.Where(r => r.AcquisitionState is not AcquisitionState.Found).ToList());

        Dispatcher.UIThread.Post(() =>
        {
            Blueprints.AddRange(missing);
            IsLoading = false;
        });
    }

    private List<string> GetBinFiles()
    {
        var sceneryPath = Path.Join(Route.DirectoryPath, "Scenery");

        if (Paths.Exists(sceneryPath))
        {
            return Directory.EnumerateFiles(sceneryPath, "*.bin").ToList();
        }

        var archivePath = Route.MainContentArchivePath;

        if (Paths.Exists(archivePath) is false)
        {
            throw new Exception($"Could not find archive at {archivePath}");
        }

        Archives.ExtractDirectory(archivePath, "Scenery");

        if (Paths.Exists(sceneryPath))
        {
            return Directory.EnumerateFiles(sceneryPath, "*.bin").ToList();
        }

        return Directory.EnumerateFiles(sceneryPath, "*.bin").ToList();
    }

    public void OnClose()
    {
        _cts.Cancel();
    }
}
