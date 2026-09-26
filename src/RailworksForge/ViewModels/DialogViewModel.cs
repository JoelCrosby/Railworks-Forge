using System;

using CommunityToolkit.Mvvm.Input;

namespace RailworksForge.ViewModels;

public abstract partial class DialogViewModel : ViewModelBase
{
    public event Action<object?>? CloseRequested;

    protected void RequestClose(object? result)
    {
        CloseRequested?.Invoke(result);
    }

    [RelayCommand]
    private void Dismiss()
    {
        RequestClose(null);
    }
}

public abstract class DialogViewModel<TResult> : DialogViewModel
{
    protected void Close(TResult result)
    {
        RequestClose(result);
    }
}
