using System;
using System.Reactive;
using System.Reactive.Linq;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Views.Controls;

using ReactiveUI;

namespace RailworksForge.ViewModels;

public partial class RoutesViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ShowListCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowGridCommand { get; }

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private UserControl _contentControl;

    public RoutesViewModel(IServiceProvider provider)
    {
        var context = provider.GetRequiredService<RoutesBaseViewModel>();

        var grid = new RoutesGrid
        {
            DataContext = context,
        };

        var list = new RoutesList
        {
            DataContext = context,
        };

        ShowGridCommand = ReactiveCommand.Create(() =>
        {
            ContentControl = grid;
        });

        ShowListCommand = ReactiveCommand.Create(() =>
        {
            ContentControl = list;
        });

        this.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName is not nameof(SearchTerm)) return;

            await context.GetAllRoutes(SearchTerm);
        };

        ContentControl = grid;

        Observable.FromAsync(() => context.GetAllRoutes(), RxApp.TaskpoolScheduler).Subscribe();
    }
}
