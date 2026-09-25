using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using ReactiveUI;

using Serilog;

namespace RailworksForge.ViewModels;

public partial class LoadingOperation : ObservableObject
{
    private CancellationTokenSource? _cancellation;
    private Func<Task>? _retry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    private bool _isLoading;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    private string? _errorMessage;

    public bool IsIdle => !IsLoading;

    public bool HasError => ErrorMessage is not null;

    public bool IsVisible => IsLoading || HasError;

    public bool CanRetry => HasError && _retry is not null;

    public ReactiveCommand<Unit, Unit> RetryCommand { get; }

    public LoadingOperation()
    {
        RetryCommand = ReactiveCommand.CreateFromTask(() => _retry?.Invoke() ?? Task.CompletedTask);
    }

    // Start on the UI thread so completion and property notifications return to that context.
    public async Task RunAsync<T>(
        string message,
        Func<CancellationToken, Task<T>> load,
        Action<T> apply,
        bool allowRetry = true)
    {
        Cancel();
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        _retry = allowRetry ? () => RunAsync(message, load, apply) : null;
        Message = message;
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var result = await Task.Run(() => load(cancellation.Token), cancellation.Token);

            var isCurrent = _cancellation == cancellation && !cancellation.IsCancellationRequested;

            if (isCurrent)
            {
                apply(result);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Log.Error(exception, "{Operation} failed", message);

            if (_cancellation == cancellation)
            {
                ErrorMessage = exception.Message;
            }
        }
        finally
        {

            if (_cancellation == cancellation)
            {
                _cancellation = null;
                IsLoading = false;
            }
        }
    }

    public Task RunAsync(string message, Func<CancellationToken, Task> work)
    {
        return RunAsync(message, async token =>
        {
            await work(token);

            return true;
        }, _ => { }, allowRetry: false);
    }

    public void Cancel()
    {
        var cancellation = _cancellation;
        _cancellation = null;
        cancellation?.Cancel();
        _retry = null;
        ErrorMessage = null;
        IsLoading = false;
    }
}
