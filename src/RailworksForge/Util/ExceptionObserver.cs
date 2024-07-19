using System;

using Serilog;

namespace RailworksForge.Util;

internal class ExceptionObserver : IObserver<Exception>
{
    public void OnCompleted() {}

    public void OnError(Exception error)
    {
        Log.Error(error, "ReactiveUI subscription exception thrown");
    }

    public void OnNext(Exception value) {}
}
