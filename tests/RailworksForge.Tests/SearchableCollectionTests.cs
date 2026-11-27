using System.Collections.Specialized;

using RailworksForge.Util;

namespace RailworksForge.Tests;

public class SearchableCollectionTests
{
    [Fact]
    public void Filter_ShowsOnlyMatchingItems()
    {
        var collection = new SearchableCollection<string>(item => item.ToLowerInvariant());
        collection.Reset(["Class 390", "Class 66", "HST"]);

        collection.Filter("CLASS");

        Assert.Equal(["Class 390", "Class 66"], collection);
    }

    [Fact]
    public void Reset_KeepsTheCurrentFilter()
    {
        var collection = new SearchableCollection<string>(item => item.ToLowerInvariant());
        collection.Filter("hst");

        collection.Reset(["Class 390", "HST"]);

        Assert.Equal(["HST"], collection);
        Assert.Equal(2, collection.Source.Count);
    }

    [Fact]
    public void BlankFilter_ShowsEverything()
    {
        var collection = new SearchableCollection<string>(item => item.ToLowerInvariant());
        collection.Reset(["Class 390", "HST"]);
        collection.Filter("hst");

        collection.Filter("  ");

        Assert.Equal(["Class 390", "HST"], collection);
    }

    [Fact]
    public void Filter_RaisesASingleResetNotification()
    {
        var collection = new SearchableCollection<string>(item => item.ToLowerInvariant());
        collection.Reset(["Class 390", "Class 66", "HST"]);
        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, args) => actions.Add(args.Action);

        collection.Filter("class");

        Assert.Equal([NotifyCollectionChangedAction.Reset], actions);
    }

    [Fact]
    public void Where_NarrowsTheItemsAlongsideTheSearchTerm()
    {
        var collection = new SearchableCollection<string>(item => item.ToLowerInvariant());
        collection.Reset(["Class 390", "Class 66", "HST"]);

        collection.Where(item => item != "Class 66");
        collection.Filter("class");

        Assert.Equal(["Class 390"], collection);

        collection.Where(null);

        Assert.Equal(["Class 390", "Class 66"], collection);
    }

    [Fact]
    public void Where_KeepsApplyingAfterAReset()
    {
        var collection = new SearchableCollection<string>(item => item.ToLowerInvariant());
        collection.Where(item => item.StartsWith('C'));

        collection.Reset(["Class 390", "HST"]);

        Assert.Equal(["Class 390"], collection);
        Assert.Equal(2, collection.Source.Count);
    }
}
