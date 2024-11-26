namespace RailworksForge.ViewModels;

public class DesignCheckAssetsViewModel : CheckAssetsViewModel
{
    public DesignCheckAssetsViewModel() : base(DesignData.DesignData.Route)
    {
        LoadingMessage = "Processing 24 of 100 files ( %24 )";
        LoadingStatusMessage = "Processing file: /cache/file/path/binary.bin";
        LoadingProgress = 24;
    }
}
