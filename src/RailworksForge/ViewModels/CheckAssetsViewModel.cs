using System;
using System.Collections.Concurrent;
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
    private string _loadingStatusMessage;

    [ObservableProperty]
    private ObservableCollection<Blueprint> _blueprints;

    private readonly CancellationTokenSource _cts = new ();

    public CheckAssetsViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        Blueprints = [];
        LoadingMessage = string.Empty;
        LoadingStatusMessage = string.Empty;

        if (Design.IsDesignMode)
        {
            return;
        }

        Observable.Start(GetMissingAssets, RxApp.TaskpoolScheduler);
    }

    private async Task GetMissingAssets()
    {
        var binFiles = GetBinFiles();

        var results = new ConcurrentDictionary<Blueprint, byte>();

        var processedCount = 0;
        var amountToProcess = binFiles.Count;

        await Parallel.ForEachAsync(binFiles, _cts.Token, async (path, token) =>
        {
            try
            {
                var serialised = await Serz.Convert(path, token);
                var xml = File.OpenRead(serialised.OutputPath);

                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
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

                    results.TryAdd(blueprint, byte.MinValue);
                }

                processedCount++;

                var count = processedCount;

                LoadingProgress = (int) Math.Ceiling((double)(100 * count) / amountToProcess);
                LoadingMessage = $"Processed {count} of {amountToProcess} files ( %{LoadingProgress} )";
                LoadingStatusMessage = $"Processing path: {path}";
            }
            catch (Exception e)
            {
                Log.Error(e, "check assets for path path failed");
            }
        });

        var amountCheckedCount = 0;
        var amountToCheck = results.Count;

        var missing = await Observable.Start(() =>
        {
            var notFound = new List<Blueprint>();

            foreach (var blueprint in results.Keys)
            {
                amountCheckedCount++;

                var count = amountCheckedCount;

                Dispatcher.UIThread.Post(() =>
                {
                    LoadingProgress = (int) Math.Ceiling((double)(100 * count) / amountToCheck);
                    LoadingMessage = $"Checked {count} of {amountToCheck} blueprints ( %{LoadingProgress} )";
                    LoadingStatusMessage = $"Processing blueprint at path: {blueprint.BinaryPath}";
                });

                if (blueprint.AcquisitionState is not AcquisitionState.Found)
                {
                    notFound.Add(blueprint);
                }
            }

            return notFound.OrderBy(n => n.BlueprintSetIdProvider);

        }, RxApp.TaskpoolScheduler);

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
