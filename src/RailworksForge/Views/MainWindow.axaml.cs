using System;
using System.Threading.Tasks;

using Avalonia.ReactiveUI;

using RailworksForge.Core.Models;
using RailworksForge.ViewModels;
using RailworksForge.Views.Controls;

using ReactiveUI;

namespace RailworksForge.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(action =>
        {
            if (ViewModel is null)
            {
                throw new Exception("view model not instantiated");
            }

            action(ViewModel.ShowSaveConsistDialog.RegisterHandler(ShowDialog
                <SaveConsistViewModel, SaveConsistViewModel, SaveConsistDialog>));

            action(ViewModel.ShowReplaceConsistDialog.RegisterHandler(ShowDialog
                <ReplaceConsistViewModel, SavedConsist, ReplaceConsistDialog>));
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
