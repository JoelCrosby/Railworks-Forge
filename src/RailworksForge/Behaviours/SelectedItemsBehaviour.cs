using System;
using System.Collections;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace RailworksForge.Behaviours;

// A control's SelectedItems is not bindable, so mirror it into a view model collection. Changes the view model makes,
// such as clearing the selection, are pushed back to the control.
public abstract class SelectedItemsBehaviour<TControl> : StyledElementBehavior<TControl>
    where TControl : Control
{
    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<SelectedItemsBehaviour<TControl>, IList?>(nameof(SelectedItems));

    private bool _isSyncing;

    public IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            SubscribeToSelection(AssociatedObject, OnSelectionChanged);
        }

        Subscribe(SelectedItems);
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            UnsubscribeFromSelection(AssociatedObject, OnSelectionChanged);
        }

        Unsubscribe(SelectedItems);

        base.OnDetaching();
    }

    protected abstract IList GetControlSelection(TControl control);

    protected abstract void SubscribeToSelection(TControl control, EventHandler<SelectionChangedEventArgs> handler);

    protected abstract void UnsubscribeFromSelection(TControl control, EventHandler<SelectionChangedEventArgs> handler);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != SelectedItemsProperty)
        {
            return;
        }

        Unsubscribe(change.OldValue as IList);
        Subscribe(change.NewValue as IList);
    }

    private void Subscribe(IList? items)
    {
        if (items is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnViewModelSelectionChanged;
        }
    }

    private void Unsubscribe(IList? items)
    {
        if (items is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged -= OnViewModelSelectionChanged;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject is null || SelectedItems is null || _isSyncing)
        {
            return;
        }

        _isSyncing = true;

        try
        {
            // Applying just the change keeps a single click from re-adding every selected row, each of which notifies.
            foreach (var item in e.RemovedItems)
            {
                SelectedItems.Remove(item);
            }

            foreach (var item in e.AddedItems)
            {
                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }

            var controlSelection = GetControlSelection(AssociatedObject);
            var isInSync = SelectedItems.Count == controlSelection.Count;

            if (!isInSync)
            {
                RebuildSelectedItems(controlSelection, SelectedItems);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void OnViewModelSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (AssociatedObject is null || SelectedItems is null || _isSyncing)
        {
            return;
        }

        _isSyncing = true;

        try
        {
            var controlSelection = GetControlSelection(AssociatedObject);

            controlSelection.Clear();

            foreach (var item in SelectedItems)
            {
                controlSelection.Add(item);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private static void RebuildSelectedItems(IList controlSelection, IList selectedItems)
    {
        selectedItems.Clear();

        foreach (var item in controlSelection)
        {
            selectedItems.Add(item);
        }
    }
}
