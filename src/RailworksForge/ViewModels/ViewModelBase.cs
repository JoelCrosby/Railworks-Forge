using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RailworksForge.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private LoadingOperation? _loading;

    public LoadingOperation Loading => _loading ??= CreateLoadingOperation();

    public bool IsLoading => Loading.IsLoading;

    public bool IsActive { get; private set; }

    public Task Activate()
    {
        IsActive = true;

        return OnActivated();
    }

    public void Deactivate()
    {
        IsActive = false;
        _loading?.Cancel();

        OnDeactivated();
    }

    protected virtual Task OnActivated()
    {
        return Task.CompletedTask;
    }

    protected virtual void OnDeactivated()
    {
    }

    private LoadingOperation CreateLoadingOperation()
    {
        var loading = new LoadingOperation();

        loading.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Loading.IsLoading))
            {
                OnPropertyChanged(nameof(IsLoading));
            }
        };

        return loading;
    }
}
