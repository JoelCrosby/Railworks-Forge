using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using RailworksForge.ViewModels;

namespace RailworksForge;

public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> Registration = new ();

    public static void Register<TViewModel, TView>()
        where TViewModel : ViewModelBase
        where TView : Control, new()
    {
        Registration.Add(typeof(TViewModel), () => new TView());
    }

    public static Control CreateView(object viewModel)
    {
        var type = viewModel.GetType();

        if (Registration.TryGetValue(type, out var factory))
        {
            return factory();
        }

        return new TextBlock { Text = "Not Found: " + type };
    }

    public bool Match(object? data) => data is ViewModelBase;

    public Control Build(object? data)
    {
        return data is null ? new TextBlock { Text = "Not Found" } : CreateView(data);
    }
}
