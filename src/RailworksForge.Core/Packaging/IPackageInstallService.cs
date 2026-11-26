namespace RailworksForge.Core.Packaging;

public interface IPackageInstallService
{
    event Action? Installed;

    Task Install(IReadOnlyList<string> packagePaths, IProgress<InstallProgress> progress);
}
