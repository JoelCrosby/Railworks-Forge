using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace RailworksForge.Util;

public class SearchableCollection<T> : ObservableCollection<T>
{
    private readonly Func<T, string, bool> _matches;
    private List<T> _source = [];
    private string? _searchTerm;
    private Func<T, bool>? _predicate;

    public SearchableCollection(Func<T, string> searchIndex)
        : this((item, term) => searchIndex(item).Contains(term))
    {
    }

    public SearchableCollection(Func<T, string, bool> matches)
    {
        _matches = matches;
    }

    public IReadOnlyList<T> Source => _source;

    public void Reset(IEnumerable<T> items)
    {
        _source = items.ToList();
        ApplyFilter();
    }

    public void Filter(string? searchTerm)
    {
        _searchTerm = searchTerm?.ToLowerInvariant();
        ApplyFilter();
    }

    public void Where(Func<T, bool>? predicate)
    {
        _predicate = predicate;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_searchTerm);
        var searched = hasSearchTerm ? _source.Where(item => _matches(item, _searchTerm!)) : _source;
        var predicate = _predicate;
        var visible = predicate is null ? searched : searched.Where(predicate);

        Items.Clear();

        foreach (var item in visible)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
