using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;

using RailworksForge.Core;
using RailworksForge.Util;

using ReactiveUI;

using Serilog;

namespace RailworksForge;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"{Paths.GetLoggingPath()}/railworks-forge-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            TaskScheduler.UnobservedTaskException += HandleUnobservedTaskExceptions;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
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

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
            .With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Vulkan] })
            .LogToTrace()
            .UseReactiveUI()
            .UsePlatformDetect();

    private static void HandleUnobservedTaskExceptions(object? _, UnobservedTaskExceptionEventArgs args)
    {
        args.SetObserved();

        args.Exception.Handle(ex =>
        {
            Log.Error(ex, "Unobserved task exception");
            return true;
        });
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Error(ex, "Unobserved task exception");
        }
        else
        {
            Log.Error("Unobserved task exception type: {ExceptionType}", e.ExceptionObject.GetType());
        }
    }
}
