using System.IO;

namespace RailworksForge.ViewModels;

public record DirectoryItem(string Name, DirectoryInfo Directory)
{
    public override string ToString() => Name;
}
