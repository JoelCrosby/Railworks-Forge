using System.Collections.Specialized;

using RailworksForge.Util;

namespace RailworksForge.UnitTests;

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
}
