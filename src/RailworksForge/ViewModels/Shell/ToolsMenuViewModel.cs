using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Packaging;
using RailworksForge.Services;
using RailworksForge.Translations;

namespace RailworksForge.ViewModels;

public partial class ToolsMenuViewModel(
    ToolsActivity tools,
    StoragePickerService picker,
    SerzFileService serz,
    Packager packager,
    RouteService routes,
    AssetDirectoryTreeService directoryTree,
    ImageService images) : ObservableObject
{
    private static FilePickerFileType BinaryFiles => new(Strings.serz_binary_files.CurrentValue) { Patterns = ["*.bin"] };
    private static FilePickerFileType XmlFiles => new(Strings.serz_xml_files.CurrentValue) { Patterns = ["*.xml"] };
    private static FilePickerFileType PackageFiles => new(Strings.packages.CurrentValue) { Patterns = ["*.rwp", "*.rpk"] };

    public LoadingOperation Operations => tools.Loading;

    [RelayCommand]
    private async Task ConvertBinToXml()
    {
        var path = await picker.PickFile(Strings.select_bin_file.CurrentValue, BinaryFiles);
        var isBinary = path is not null && Path.GetExtension(path) is ".bin";

        if (!isBinary || Operations.IsLoading)
        {
            return;
        }

        await Operations.RunAsync(Strings.converting_file.CurrentValue, token => serz.ConvertBinToXml(path!, token));
    }

    [RelayCommand]
    private async Task ConvertXmlToBin()
    {
        var path = await picker.PickFile(Strings.select_xml_file.CurrentValue, XmlFiles);
        var isXml = path is not null && Path.GetExtension(path) is ".xml";

        if (!isXml || Operations.IsLoading)
        {
            return;
        }

        await Operations.RunAsync(Strings.converting_file.CurrentValue, token => serz.ConvertXmlToBin(path!, token));
    }

    [RelayCommand]
    private async Task InstallPackage()
    {
        var files = await picker.PickFiles(Strings.select_package_files.CurrentValue, PackageFiles);

        if (files.Count is 0)
        {
            return;
        }

        var progress = new Progress<InstallProgress>(tools.Progress.UpdateProgress);

        try
        {
            await Operations.RunAsync(Strings.installing_packages.CurrentValue, async _ =>
            {
                foreach (var file in files)
                {
                    await packager.InstallPackage(file, progress);
                }
            });
        }
        finally
        {
            tools.Progress.ClearProgress();

            Cache.ClearAssetCaches();
            directoryTree.Invalidate();
            images.ClearMisses();
            routes.InvalidateRoutes();
        }
    }
}
