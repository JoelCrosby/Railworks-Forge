namespace RailworksForge.ViewModels;

public class DesignStatusBarViewModel : StatusBarViewModel
{
    public DesignStatusBarViewModel()
    {
        StatusText = "84 Routes found";
        ShowProgress = true;
        Progress = 0.42f;
        ProgressText = "Extracting scenario .ap file to .xml";
    }
}
