namespace RailworksForge.ViewModels;

public class DesignCheckAssetsViewModel : CheckAssetsViewModel
{
    public DesignCheckAssetsViewModel() : base(DesignData.DesignData.Route)
    {
        LoadingMessage = "Processing 24 of 100 files ( %24 )";
        LoadingProgress = 24;
    }
}
