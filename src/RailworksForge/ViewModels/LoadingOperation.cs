using System;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

namespace RailworksForge.ViewModels;

public partial class LoadingOperation : ObservableObject
{
    private CancellationTokenSource? _cancellation;
    private Func<Task>? _retry;

    // Raised for failures with nowhere else to be shown, such as work (as opposed to a load) that fails after its
    // page was left, so they aren't silently lost.
    public static event Action<string>? UnobservedFailure;

    public static void ReportUnobservedFailure(string message)
    {
        UnobservedFailure?.Invoke(message);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    [NotifyPropertyChangedFor(nameof(CanDismiss))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    [NotifyPropertyChangedFor(nameof(CanDismiss))]
    public partial string? ErrorMessage { get; set; }

    public bool IsIdle => !IsLoading;

    public bool HasError => ErrorMessage is not null;

    public bool IsVisible => IsLoading || HasError;

    public bool CanRetry => HasError && _retry is not null;

    public bool CanDismiss => HasError && !IsLoading;

    // Start on the UI thread so completion and property notifications return to that context.
    public async Task RunAsync<T>(
        string message,
        Func<CancellationToken, Task<T>> load,
        Action<T> apply,
        bool allowRetry = true,
        bool reportDetachedFailure = false)
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
            else if (reportDetachedFailure)
            {
                ReportUnobservedFailure($"{message}: {exception.Message}");
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
        }, _ => { }, allowRetry: false, reportDetachedFailure: true);
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

    // Shows an error from outside this operation, so Retry must not re-run whatever this operation last ran.
    public void ShowError(string message)
    {
        _retry = null;
        ErrorMessage = message;
    }

    [RelayCommand]
    private void Dismiss()
    {
        ErrorMessage = null;
    }

    [RelayCommand]
    private Task Retry()
    {
        return _retry?.Invoke() ?? Task.CompletedTask;
    }
}
