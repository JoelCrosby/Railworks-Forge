using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

using LucideAvalonia.Enum;

namespace RailworksForge.Views.Components;

[PseudoClasses(":current", ":dirty")]
public class BreadcrumbItem : TemplatedControl
{
    public static readonly StyledProperty<LucideIconNames> IconProperty =
        AvaloniaProperty.Register<BreadcrumbItem, LucideIconNames>(nameof(Icon));

    public static readonly StyledProperty<IBrush?> IconBackgroundProperty =
        AvaloniaProperty.Register<BreadcrumbItem, IBrush?>(nameof(IconBackground));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<BreadcrumbItem, string?>(nameof(Text));

    public static readonly StyledProperty<bool> IsCurrentProperty =
        AvaloniaProperty.Register<BreadcrumbItem, bool>(nameof(IsCurrent));

    public static readonly StyledProperty<bool> IsDirtyProperty =
        AvaloniaProperty.Register<BreadcrumbItem, bool>(nameof(IsDirty));

    public static readonly StyledProperty<bool> HasSeparatorProperty =
        AvaloniaProperty.Register<BreadcrumbItem, bool>(nameof(HasSeparator), true);

    public static readonly StyledProperty<string?> DirtyTextProperty =
        AvaloniaProperty.Register<BreadcrumbItem, string?>(nameof(DirtyText));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<BreadcrumbItem, ICommand?>(nameof(Command));

    public LucideIconNames Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IBrush? IconBackground
    {
        get => GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsCurrent
    {
        get => GetValue(IsCurrentProperty);
        set => SetValue(IsCurrentProperty, value);
    }

    public bool IsDirty
    {
        get => GetValue(IsDirtyProperty);
        set => SetValue(IsDirtyProperty, value);
    }

    public bool HasSeparator
    {
        get => GetValue(HasSeparatorProperty);
        set => SetValue(HasSeparatorProperty, value);
    }

    public string? DirtyText
    {
        get => GetValue(DirtyTextProperty);
        set => SetValue(DirtyTextProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCurrentProperty)
        {
            PseudoClasses.Set(":current", IsCurrent);
        }
        else if (change.Property == IsDirtyProperty)
        {
            PseudoClasses.Set(":dirty", IsDirty);
        }
    }
}
