using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

using LucideAvalonia.Enum;

namespace RailworksForge.Views.Components;

public class PageHeader : TemplatedControl
{
    public static readonly StyledProperty<IImage?> ImageProperty =
        AvaloniaProperty.Register<PageHeader, IImage?>(nameof(Image));

    public static readonly StyledProperty<LucideIconNames> PlaceholderIconProperty =
        AvaloniaProperty.Register<PageHeader, LucideIconNames>(nameof(PlaceholderIcon), LucideIconNames.TrainFront);

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PageHeader, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<PageHeader, string?>(nameof(Description));

    public static readonly StyledProperty<object?> BadgesProperty =
        AvaloniaProperty.Register<PageHeader, object?>(nameof(Badges));

    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<PageHeader, object?>(nameof(Actions));

    public IImage? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public LucideIconNames PlaceholderIcon
    {
        get => GetValue(PlaceholderIconProperty);
        set => SetValue(PlaceholderIconProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? Badges
    {
        get => GetValue(BadgesProperty);
        set => SetValue(BadgesProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }
}
