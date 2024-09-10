namespace RailworksForge.ViewModels;

public class DesignProgressIndicatorViewModel : ProgressIndicatorViewModel
{
    public DesignProgressIndicatorViewModel()
    {
        IsLoading = true;
        Progress = 34;
        ProgressMessage = "Loading...";
        StatusMessage = "Installing package 2 of 8...";
    }
}
