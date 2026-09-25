using CommunityToolkit.Mvvm.ComponentModel;

namespace RailworksForge.ViewModels;

public class ViewModelBase : ObservableObject
{
    private LoadingOperation? _loading;

    public LoadingOperation Loading => _loading ??= CreateLoadingOperation();

    public bool IsLoading
    {
        get => Loading.IsLoading;
        set => Loading.IsLoading = value;
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

    public bool IsActive { get; private set; } = true;

    public virtual void Activate()
    {
        IsActive = true;
    }

    public virtual void CancelLoading()
    {
        IsActive = false;
        _loading?.Cancel();
    }
}
