using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;

using RailworksForge.Core;
using RailworksForge.Util;

using ReactiveUI;

using Serilog;
using Serilog.Events;

namespace RailworksForge;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Debug)
                .WriteTo.File($"{Paths.GetLoggingPath()}/railworks-forge-.log", LogEventLevel.Error)
                .CreateLogger();

            TaskScheduler.UnobservedTaskException += HandleUnobservedTaskExceptions;

            RxApp.DefaultExceptionHandler = new ExceptionObserver();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Something very bad happened");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    private static void HandleUnobservedTaskExceptions(object? _, UnobservedTaskExceptionEventArgs args)
    {
        args.SetObserved();

        args.Exception.Handle(ex =>
        {
            Log.Error("Unobserved task exception type: {ExceptionType}", ex.GetType());
            return true;
        });
    }
}
