using System;
using System.Diagnostics;
using System.Reactive.Concurrency;

using ReactiveUI;

using Serilog;

namespace RailworksForge.Util;

internal class ExceptionObserver : IObserver<Exception>
{
    public void OnCompleted() {}

    public void OnError(Exception value)
    {
        if (Debugger.IsAttached) Debugger.Break();

        Log.Error(value, "ReactiveUI subscription exception thrown");

        RxApp.MainThreadScheduler.Schedule(() => throw value) ;
    }

    public void OnNext(Exception value)
    {
        if (Debugger.IsAttached) Debugger.Break();

        Log.Error(value, "ReactiveUI subscription exception thrown");

        RxApp.MainThreadScheduler.Schedule(() => throw value) ;
    }
}
