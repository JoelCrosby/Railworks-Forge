using System;
using System.Threading.Tasks;

using Avalonia.ReactiveUI;

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

            action(ViewModel!.ShowSaveConsistDialog.RegisterHandler(DoShowDialogAsync));
        });
    }

    private async Task DoShowDialogAsync(InteractionContext<SaveConsistViewModel, SaveConsistViewModel?> interaction)
    {
        var dialog = new SaveConsistDialog
        {
            DataContext = interaction.Input,
        };

        var result = await dialog.ShowDialog<SaveConsistViewModel?>(this);

        interaction.SetOutput(result);
    }
}
