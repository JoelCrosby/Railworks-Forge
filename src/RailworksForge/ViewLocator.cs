using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using RailworksForge.ViewModels;

namespace RailworksForge;

public class ViewLocator : IDataTemplate
{
    public bool Match(object? data) => data is ViewModelBase;

    private static readonly Dictionary<Type, Func<Control>> Registration = new ();

    public static void Register<TViewModel, TView>()
        where TView : Control, new()
    {
        Registration.Add(typeof(TViewModel), () => new TView());
    }

    public Control Build(object? data)
    {
        var type = data?.GetType();

        if (type is not null && Registration.TryGetValue(type, out var factory))
        {
            return factory();
        }

        return new TextBlock { Text = "Not Found: " + type };
    }
}
