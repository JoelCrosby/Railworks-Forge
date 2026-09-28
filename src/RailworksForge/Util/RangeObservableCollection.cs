using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RailworksForge.Util;

// Raises a single Reset for a bulk change, instead of one Add notification (and grid/tree update) per item.
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Items.Add(item);
        }

        NotifyReset();
    }

    public void ReplaceWith(IEnumerable<T> items)
    {
        Items.Clear();
        AddRange(items);
    }

    private void NotifyReset()
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
