using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace RailworksForge.Views.Components;

[TemplatePart("PART_TextBox", typeof(TextBox))]
public class SearchBox : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> ShortcutHintProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(ShortcutHint));

    // Only one search box per window should answer Ctrl+F, so pages opt in on their main search.
    public static readonly StyledProperty<bool> IsShortcutTargetProperty =
        AvaloniaProperty.Register<SearchBox, bool>(nameof(IsShortcutTarget));

    private TextBox? _textBox;
    private TopLevel? _topLevel;

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string? ShortcutHint
    {
        get => GetValue(ShortcutHintProperty);
        set => SetValue(ShortcutHintProperty, value);
    }

    public bool IsShortcutTarget
    {
        get => GetValue(IsShortcutTargetProperty);
        set => SetValue(IsShortcutTargetProperty, value);
    }

    public void FocusSearch()
    {
        _textBox?.Focus();
        _textBox?.SelectAll();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel = null;
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        var isSearchShortcut = e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control;
        var canTakeShortcut = IsShortcutTarget && IsEffectivelyVisible && IsEffectivelyEnabled;

        if (!isSearchShortcut || !canTakeShortcut)
        {
            return;
        }

        FocusSearch();
        e.Handled = true;
    }
}
