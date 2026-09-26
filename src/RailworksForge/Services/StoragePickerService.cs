using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using RailworksForge.Util;

namespace RailworksForge.Services;

public class StoragePickerService
{
    public async Task<string?> PickFile(string title, FilePickerFileType fileType)
    {
        var files = await PickFiles(title, fileType, false);

        return files.FirstOrDefault();
    }

    public Task<List<string>> PickFiles(string title, FilePickerFileType fileType)
    {
        return PickFiles(title, fileType, true);
    }

    public async Task<string?> PickFolder(string title)
    {
        var provider = Utils.GetApplicationWindow().StorageProvider;

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.FirstOrDefault()?.Path.LocalPath;
    }

    private static async Task<List<string>> PickFiles(string title, FilePickerFileType fileType, bool allowMultiple)
    {
        var provider = Utils.GetApplicationWindow().StorageProvider;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = [fileType],
        });

        return files.Select(file => file.Path.LocalPath).ToList();
    }
}
