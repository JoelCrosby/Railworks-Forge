using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;

using RailworksForge.Core;

using Serilog;
using Serilog.Events;

namespace RailworksForge;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Debug)
                .WriteTo.File(Paths.GetLoggingPath(), LogEventLevel.Error)
                .CreateLogger();

            TaskScheduler.UnobservedTaskException += HandleUnobservedTaskExceptions;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Something very bad happened");
        }
        finally
        {
            // This block is optional.
            // Use the finally-block if you need to clean things up or similar
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
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
