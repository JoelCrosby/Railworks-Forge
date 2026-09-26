using RailworksForge.ViewModels;

namespace RailworksForge.Services;

public class ToolsActivity
{
    public LoadingOperation Loading { get; } = new();

    public ProgressIndicatorViewModel Progress { get; } = new();
}
