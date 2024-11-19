using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace RailworksForge.Behaviours;

public class AutoCompleteBehaviour : Behavior<AutoCompleteBox>
{
    [RequiresUnreferencedCode("This functionality is not compatible with trimming.")]
    protected override void OnAttached()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.KeyUp += OnKeyUp;
            AssociatedObject.DropDownOpening += DropDownOpening;
            AssociatedObject.GotFocus += OnGotFocus;
            AssociatedObject.PointerReleased += PointerReleased;

            Task.Delay(500).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Invoke(CreateDropdownButton));
        }

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.KeyUp -= OnKeyUp;
            AssociatedObject.DropDownOpening -= DropDownOpening;
            AssociatedObject.GotFocus -= OnGotFocus;
            AssociatedObject.PointerReleased -= PointerReleased;
        }

        base.OnDetaching();
    }

    // have to use KeyUp as AutoCompleteBox eats some of the KeyDown events
    private void OnKeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key is Avalonia.Input.Key.Down or Avalonia.Input.Key.F4)
        {
            if (string.IsNullOrEmpty(AssociatedObject?.Text))
            {
                ShowDropdown();
            }
        }
    }

    private void DropDownOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var prop = AssociatedObject?.GetType().GetProperty("TextBox", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var tb = (TextBox?)prop?.GetValue(AssociatedObject);

        if (tb is not null && tb.IsReadOnly)
        {
            e.Cancel = true;
        }
    }

    private void PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (string.IsNullOrEmpty(AssociatedObject?.Text))
        {
            ShowDropdown();
        }
    }

    private void ShowDropdown()
    {
        if (AssociatedObject is null || AssociatedObject.IsDropDownOpen) return;

        var autoCompleteBoxType = typeof(AutoCompleteBox);

        autoCompleteBoxType.GetMethod("PopulateDropDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(AssociatedObject, [AssociatedObject, EventArgs.Empty]);
        autoCompleteBoxType.GetMethod("OpeningDropDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(AssociatedObject, [false]);

        if (AssociatedObject.IsDropDownOpen) return;

        // We *must* set the field and not the property as we need to avoid the changed event being raised (which prevents the dropdown opening).
        var ipc = autoCompleteBoxType.GetField("_ignorePropertyChange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if ((bool?) ipc?.GetValue(AssociatedObject) == false)
        {
            ipc.SetValue(AssociatedObject, true);
        }

        AssociatedObject.SetCurrentValue(AutoCompleteBox.IsDropDownOpenProperty, true);
    }

    private void CreateDropdownButton()
    {
        if (AssociatedObject is null) return;

        var prop = AssociatedObject.GetType().GetProperty("TextBox", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var tb = (TextBox?)prop?.GetValue(AssociatedObject);

        if (tb is null || tb.InnerRightContent is Button)
        {
            return;
        }

        var btn = new Button
        {
            Content = "⯆",
            Margin = new(3),
            ClickMode = ClickMode.Press,
        };

        btn.Click += (_, _) =>
        {
            AssociatedObject.Text = string.Empty;
            ShowDropdown();
        };

        tb.InnerRightContent = btn;
    }

    private void OnGotFocus(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject != null)
        {
            CreateDropdownButton();
        }
    }
}
