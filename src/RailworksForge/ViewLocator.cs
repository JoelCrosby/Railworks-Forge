using System;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using RailworksForge.ViewModels;

namespace RailworksForge;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var typeName = data.GetType().Name;
        var name = typeName.Replace("ViewModel", "", StringComparison.Ordinal);
        var fullName = data.GetType().FullName!.Replace("ViewModels", "Views.Controls").Replace(typeName, name);
        var type = Type.GetType(fullName);

        if (type is null)
        {
            return new TextBlock
            {
                Text = $"Not Found: {name}",
            };
        }


        if (Activator.CreateInstance(type) is Control control)
        {
            control.DataContext = data;
            return control;
        }

        throw new Exception("failed to cast control type in view locator");
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
