using System;
using System.Threading.Tasks;

using Avalonia.Controls;

using Microsoft.Extensions.DependencyInjection;

using RailworksForge.Util;
using RailworksForge.ViewModels;

namespace RailworksForge.Services;

public class DialogService(IServiceProvider services)
{
    public TDialog Create<TDialog>(params object[] arguments) where TDialog : DialogViewModel
    {
        return ActivatorUtilities.CreateInstance<TDialog>(services, arguments);
    }

    public async Task<TResult?> Show<TResult>(DialogViewModel<TResult> dialog)
    {
        var result = await ShowWindow(dialog);

        return result is TResult typedResult ? typedResult : default;
    }

    public Task Show(DialogViewModel dialog)
    {
        return ShowWindow(dialog);
    }

    private static async Task<object?> ShowWindow(DialogViewModel dialog)
    {
        var owner = Utils.GetApplicationWindow();
        var window = (Window)ViewLocator.CreateView(dialog);

        window.DataContext = dialog;
        dialog.CloseRequested += window.Close;

        try
        {
            _ = dialog.Activate();

            return await window.ShowDialog<object?>(owner);
        }
        finally
        {
            dialog.CloseRequested -= window.Close;
            dialog.Deactivate();
        }
    }
}
