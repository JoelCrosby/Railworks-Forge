namespace RailworksForge.Core.Config;

public interface IGameDirectoryService
{
    string Current { get; }

    bool IsCurrentValid { get; }

    bool IsValid(string? path);

    void Resolve();

    GameDirectoryChange Change(string path);
}
