using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using RailworksForge.Core;
using RailworksForge.Core.Packaging;
using RailworksForge.Services;

namespace RailworksForge.ViewModels;

public partial class ToolbarViewModel(
    ToolsActivity tools,
    StoragePickerService picker,
    SerzFileService serz,
    Packager packager) : ObservableObject
{
    private static readonly FilePickerFileType BinaryFiles = new("Serz binary") { Patterns = ["*.bin"] };
    private static readonly FilePickerFileType XmlFiles = new("Serz XML") { Patterns = ["*.xml"] };
    private static readonly FilePickerFileType PackageFiles = new("Package") { Patterns = ["*.rwp", "*.rpk"] };

    public LoadingOperation Operations => tools.Loading;

    [RelayCommand]
    private async Task ConvertBinToXml()
    {
        var path = await picker.PickFile("Select .bin file", BinaryFiles);
        var isBinary = path is not null && Path.GetExtension(path) is ".bin";

        if (!isBinary || Operations.IsLoading)
        {
            return;
        }

        await Operations.RunAsync("Converting file…", token => serz.ConvertBinToXml(path!, token));
    }

    [RelayCommand]
    private async Task ConvertXmlToBin()
    {
        var path = await picker.PickFile("Select .xml file", XmlFiles);
        var isXml = path is not null && Path.GetExtension(path) is ".xml";

        if (!isXml || Operations.IsLoading)
        {
            return;
        }

        await Operations.RunAsync("Converting file…", token => serz.ConvertXmlToBin(path!, token));
    }

    [RelayCommand]
    private async Task InstallPackage()
    {
        var files = await picker.PickFiles("Select .rwp or .rpk file", PackageFiles);

        if (files.Count is 0)
        {
            return;
        }

        var progress = new Progress<InstallProgress>(tools.Progress.UpdateProgress);

        try
        {
            await Operations.RunAsync("Installing packages…", async _ =>
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
        }
    }
}
