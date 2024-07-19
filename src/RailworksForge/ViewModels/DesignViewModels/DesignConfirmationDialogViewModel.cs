namespace RailworksForge.ViewModels;

public class DesignConfirmationDialogViewModel : ConfirmationDialogViewModel
{
    public DesignConfirmationDialogViewModel()
    {
        Title = "Replace Consist";
        BodyText = "Are you sure you want to replace the selected consist(s).";
        AcceptLabel = "Replace Consist";
    }
}
