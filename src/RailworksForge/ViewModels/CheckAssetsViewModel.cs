using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using RailworksForge.Core.Models;
using RailworksForge.Core.Models.Common;

namespace RailworksForge.ViewModels;

public partial class CheckAssetsViewModel : ViewModelBase
{
    [ObservableProperty]
    private Route _route;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Blueprint> _blueprints;

    public CheckAssetsViewModel(Route route)
    {
        Route = route;
        IsLoading = true;
        Blueprints = [];
    }
}
