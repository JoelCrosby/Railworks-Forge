using System.Collections.Generic;
using System.Threading.Tasks;

namespace RailworksForge.Services;

public interface IStoragePickerService
{
    Task<string?> PickFile(string title, FileTypeFilter fileType);

    Task<List<string>> PickFiles(string title, FileTypeFilter fileType);

    Task<string?> PickFolder(string title);
}
