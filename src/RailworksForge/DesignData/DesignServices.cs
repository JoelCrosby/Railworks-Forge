using System;

using Microsoft.Extensions.DependencyInjection;

namespace RailworksForge.DesignData;

public static class DesignServices
{
    private static readonly Lazy<IServiceProvider> Provider = new(CreateProvider);

    public static T Get<T>() where T : notnull
    {
        return Provider.Value.GetRequiredService<T>();
    }

    public static T Create<T>(params object[] arguments)
    {
        return ActivatorUtilities.CreateInstance<T>(Provider.Value, arguments);
    }

    private static IServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();

        App.RegisterServices(services);

        return services.BuildServiceProvider();
    }
}
