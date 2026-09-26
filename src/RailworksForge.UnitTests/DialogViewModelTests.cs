using RailworksForge.Core.Models;
using RailworksForge.ViewModels;

namespace RailworksForge.UnitTests;

public class DialogViewModelTests
{
    [Fact]
    public void ConfirmationAccept_ClosesWithTrue()
    {
        var dialog = new ConfirmationDialogViewModel();
        object? result = null;
        dialog.CloseRequested += value => result = value;

        dialog.AcceptCommand.Execute(null);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Dismiss_ClosesWithoutAResult()
    {
        var dialog = new ConfirmationDialogViewModel();
        var closed = false;
        object? result = "unset";
        dialog.CloseRequested += value =>
        {
            closed = true;
            result = value;
        };

        dialog.DismissCommand.Execute(null);

        Assert.True(closed);
        Assert.Null(result);
    }

    [Fact]
    public void SaveConsist_RequiresAName()
    {
        var dialog = new SaveConsistViewModel
        {
            LocomotiveName = "Class 390",
            ConsistElement = "<RailVehicles />",
        };

        Assert.False(dialog.SaveCommand.CanExecute(null));

        dialog.Name = "Pendolino";

        Assert.True(dialog.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveConsist_ClosesWithTheSavedConsist()
    {
        var dialog = new SaveConsistViewModel
        {
            Name = "Pendolino",
            LocomotiveName = "Class 390",
            ConsistElement = "<RailVehicles />",
        };
        object? result = null;
        dialog.CloseRequested += value => result = value;

        dialog.SaveCommand.Execute(null);

        var savedConsist = Assert.IsType<SavedConsist>(result);
        Assert.Equal("Pendolino", savedConsist.Name);
        Assert.Equal("<RailVehicles />", savedConsist.ConsistElement);
    }
}
