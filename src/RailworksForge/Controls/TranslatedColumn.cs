using System;
using System.Linq.Expressions;

using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

using RailworksForge.Util;

namespace RailworksForge.Controls;

public class TranslatedColumn<TModel, TValue> : TextColumn<TModel, TValue> where TModel : class
{
    public TranslatedColumn(
        string header,
        Expression<Func<TModel, TValue?>> getter,
        GridLength? width = null,
        TextColumnOptions<TModel>? options = null) : base(
        header,
        getter,
        width,
        options
    )
    {
        Header = header;
        TranslationService.GetProvider(header).Text.Subscribe(value => Header = value);
    }
}
