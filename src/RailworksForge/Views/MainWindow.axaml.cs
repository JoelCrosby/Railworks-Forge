using System;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.ReactiveUI;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;
using RailworksForge.Views.Dialogs;

using ReactiveUI;

namespace RailworksForge.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(action =>
        {
            if (Design.IsDesignMode)
            {
                return;
            }

            if (ViewModel is null)
            {
                throw new Exception("view model not instantiated");
            }

            action(ViewModel.ShowSaveConsistDialog.RegisterHandler(ShowDialog
                <SaveConsistViewModel, SavedConsist, SaveConsistDialog>));

            action(ViewModel.ShowReplaceConsistDialog.RegisterHandler(ShowDialog
                <ReplaceConsistViewModel, PreloadConsist, ReplaceConsistDialog>));

            action(ViewModel.ShowConfirmationDialog.RegisterHandler(ShowDialog
                <ConfirmationDialogViewModel, bool, ConfirmationDialog>));

            action(ViewModel.ShowReplaceTrackDialog.RegisterHandler(ShowDialog
                <ReplaceTrackViewModel, ReplaceTracksRequest?, ReplaceTrackDialog>));

            action(ViewModel.ShowCheckAssetsDialog.RegisterHandler(ShowDialog
                <CheckAssetsViewModel, Unit?, CheckAssetsDialog>));
        });
    }

    private async Task ShowDialog<TInput, TOutput, TDialog>(InteractionContext<TInput, TOutput?> interaction)
        where TInput : ViewModelBase
        where TDialog : ReactiveWindow<TInput>, new ()
    {
        var dialog = new TDialog
        {
            DataContext = interaction.Input,
        };

        var result = await dialog.ShowDialog<TOutput?>(this);

        interaction.SetOutput(result);
    }
}
