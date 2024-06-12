using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Threading;

using DynamicData;

using RailworksForge.Core;
using RailworksForge.Util;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public class RoutesListViewModel : ViewModelBase
{
    public ObservableCollection<RouteViewModel> ListItems { get; }

    public ReactiveCommand<Unit, Unit> CopyClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    public ReactiveCommand<Unit, Unit> DetailsClickedCommand { get; }

    public RouteViewModel? SelectedItem { get; set; }

    public RoutesListViewModel()
    {
        ListItems = new ObservableCollection<RouteViewModel>();

        CopyClickedCommand = ReactiveCommand.CreateFromTask(() =>
        {
            if (SelectedItem is null) return Task.CompletedTask;

            return Clipboard.Get().SetTextAsync(SelectedItem.Name);
        });

        OpenInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem is null) return;

            Launcher.Open(SelectedItem.Path);
        });

        DetailsClickedCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem is null) return;

            Utils.GetApplicationViewModel().SelectRoute(SelectedItem);
        });
    }

    public async Task LoadRoutes()
    {
        var items = await Task.Run(RouteService.GetRoutes);

            ListItems.AddRange(items.Select(item => new RouteViewModel(item)));

            LoadImages();
    }

    private async void LoadImages()
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8,
        };

        await Parallel.ForEachAsync(ListItems, options, async (route, _) => await route.LoadImage());
    }
}
