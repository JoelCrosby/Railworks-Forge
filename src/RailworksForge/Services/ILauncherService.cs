namespace RailworksForge.Services;

public interface ILauncherService
{
    void OpenDirectory(string path);

    void OpenOrCreateDirectory(string path);
}
