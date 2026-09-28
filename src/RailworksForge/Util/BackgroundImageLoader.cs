using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace RailworksForge.Util;

// Images come from disk or inside .ap archives, so lists are shown first and images fill in as they are found.
public sealed class BackgroundImageLoader
{
    private const int MaxParallelReads = 4;

    private CancellationTokenSource? _cancellation;

    public void Load<T>(IReadOnlyList<T> items, Func<T, Bitmap?> read, Action<T, Bitmap> apply)
    {
        Cancel();

        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;

        var options = new ParallelOptions
        {
            CancellationToken = cancellation.Token,
            MaxDegreeOfParallelism = MaxParallelReads,
        };

        _ = Task.Run(() =>
        {
            try
            {
                Parallel.ForEach(items, options, item => ReadAndApply(item, read, apply, cancellation.Token));
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Cancel()
    {
        _cancellation?.Cancel();
        _cancellation = null;
    }

    private static void ReadAndApply<T>(T item, Func<T, Bitmap?> read, Action<T, Bitmap> apply, CancellationToken token)
    {
        if (read(item) is not { } image)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!token.IsCancellationRequested)
            {
                apply(item, image);
            }
        });
    }
}
