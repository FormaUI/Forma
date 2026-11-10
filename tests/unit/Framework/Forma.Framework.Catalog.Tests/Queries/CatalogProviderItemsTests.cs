namespace Forma.Framework.Catalog.Tests.Queries;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

using Xunit;

public sealed class CatalogProviderItemsTests
{
    private sealed class FakeProvider(IReadOnlyList<ResourceInfo> all, TaskCompletionSource? gate = null) : ICatalogProvider
    {
        private readonly IReadOnlyList<ResourceInfo> _all = all;
        private readonly TaskCompletionSource? _gate = gate;

        public int CallCount { get; private set; }

        public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
        {
            CallCount++;

            if (_gate is not null)
            {
                await _gate.Task.WaitAsync(cancellationToken);
            }

            return new CatalogPage(CatalogItem.ForAll(_all.Skip(query.Skip).Take(query.Take)), _all.Count);
        }
    }

    private sealed class ItemProvider(IReadOnlyList<ICatalogItem> items, int? total = null) : ICatalogProvider
    {
        public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
            => ValueTask.FromResult(new CatalogPage(items, total ?? items.Count));
    }

    // Answers the positions a query asks for from the items it holds, recording where each slice began.
    private sealed class PositionProvider(IReadOnlyList<ICatalogItem> items, bool? hasMore = null, int? listedCount = null, bool reportsTotal = true) : ICatalogProvider
    {
        public List<int> Skips { get; } = [];

        public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
        {
            Skips.Add(query.Skip);

            return ValueTask.FromResult(new CatalogPage([.. items.Skip(query.Skip).Take(query.Take)], reportsTotal ? items.Count : null)
            {
                HasMore = hasMore,
                ListedCount = listedCount
            });
        }
    }

    private static CatalogQuery CreateQuery(int skip, int take)
        => new(null, skip, take, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

    private static CatalogItem CreateHiddenItem(string name)
        => new() { Key = name, Name = name, Visibility = CatalogItemVisibility.Hidden };

    private static ResourceInfo CreateResource(string name)
        => new($"/media/{name}");

    [Fact]
    public async Task Should_leave_hidden_items_out_of_the_items()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new ItemProvider([CatalogItem.For(CreateResource("shown")), CreateHiddenItem("hidden")], total: 6);
        var query = new CatalogQuery(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["shown"], providerItems.Items.Select(item => item.Name)),
            () => Assert.Equal(2, providerItems.ReadCount),
            () => Assert.Equal(6, providerItems.Total),
            () => Assert.Null(providerItems.ListedCount));
    }

    [Fact]
    public async Task Should_read_on_by_store_position_through_a_slice_the_filter_rejects_whole()
    {
        // Arrange — two accepted files, two rejected, and one more accepted
        using var providerItems = new CatalogProviderItems();
        var provider = new PositionProvider(
        [
            CatalogItem.For(new ImageResourceInfo("/media/a.png")),
            CatalogItem.For(new ImageResourceInfo("/media/b.png")),
            CatalogItem.For(new FileResourceInfo("/media/x.txt")),
            CatalogItem.For(new FileResourceInfo("/media/y.txt")),
            CatalogItem.For(new ImageResourceInfo("/media/c.png"))
        ]);
        var filter = CatalogResourceFilter.Parse("contentType:image");
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, CreateQuery(0, 2), filter, append: false, TestContext.Current.CancellationToken);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, CreateQuery(providerItems.ReadCount, 2), filter, append: true, TestContext.Current.CancellationToken);
        var afterRejected = (providerItems.Items.Count, providerItems.ReadCount, providerItems.HasMore);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, CreateQuery(providerItems.ReadCount, 2), filter, append: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal((2, 4, true), afterRejected),
            () => Assert.Equal(["a.png", "b.png", "c.png"], providerItems.Items.Select(item => item.Name)),
            () => Assert.Equal([0, 2, 4], provider.Skips),
            () => Assert.False(providerItems.HasMore),
            () => Assert.Null(providerItems.ListedCount));
    }

    [Fact]
    public async Task Should_keep_the_listed_count_a_provider_guarantees_until_a_slice_holds_an_item_left_out()
    {
        // Arrange — a provider claiming three listed items, whose second slice holds a hidden one
        using var providerItems = new CatalogProviderItems();
        var provider = new PositionProvider(
            [CatalogItem.For(CreateResource("one")), CatalogItem.For(CreateResource("two")), CreateHiddenItem("hidden")],
            listedCount: 3);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, CreateQuery(0, 2), filter: null, append: false, TestContext.Current.CancellationToken);
        var guaranteed = providerItems.ListedCount;

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, CreateQuery(providerItems.ReadCount, 2), filter: null, append: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(3, guaranteed),
            () => Assert.Null(providerItems.ListedCount));
    }

    [Fact]
    public async Task Should_end_the_run_at_a_slice_that_read_nothing_whatever_the_provider_says_follows()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new PositionProvider([], hasMore: true);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, CreateQuery(0, 2), filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(providerItems.HasMore);
    }

    [Fact]
    public async Task Should_take_whether_more_follows_from_the_provider_over_the_counts()
    {
        // Arrange — a store that cannot count, saying another slice follows a short one
        using var providerItems = new CatalogProviderItems();
        var provider = new PositionProvider([CatalogItem.For(CreateResource("one"))], hasMore: true, reportsTotal: false);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, CreateQuery(0, 2), filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(providerItems.HasMore);
    }

    [Fact]
    public async Task Should_start_the_counts_over_when_the_items_are_replaced()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new ItemProvider([CatalogItem.For(CreateResource("shown")), CreateHiddenItem("hidden")], total: 6);
        var query = new CatalogQuery(null, 0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: true, TestContext.Current.CancellationToken);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["shown"], providerItems.Items.Select(item => item.Name)),
            () => Assert.Equal(2, providerItems.ReadCount));
    }

    [Fact]
    public void Should_hold_nothing_before_the_first_fetch()
    {
        // Act
        using var providerItems = new CatalogProviderItems();

        // Assert
        Assert.Multiple(
            () => Assert.Empty(providerItems.Items),
            () => Assert.Null(providerItems.Total),
            () => Assert.False(providerItems.HasMore),
            () => Assert.False(providerItems.IsFetching));
    }

    [Fact]
    public async Task Should_replace_the_items_on_a_non_appending_fetch()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new FakeProvider([CreateResource("one"), CreateResource("two"), CreateResource("three")]);
        var query = new CatalogQuery(null, 0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two"], providerItems.Items.Select(resource => resource.Name)),
            () => Assert.Equal(3, providerItems.Total),
            () => Assert.True(providerItems.HasMore));
    }

    [Fact]
    public async Task Should_extend_the_items_on_an_appending_fetch()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new FakeProvider([CreateResource("one"), CreateResource("two"), CreateResource("three")]);
        var firstPage = new CatalogQuery(null, 0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        var secondPage = new CatalogQuery(null, 2, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, firstPage, filter: null, append: false, TestContext.Current.CancellationToken);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, secondPage, filter: null, append: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two", "three"], providerItems.Items.Select(resource => resource.Name)),
            () => Assert.False(providerItems.HasMore));
    }

    [Fact]
    public async Task Should_keep_a_resource_the_filter_rejects_out_of_the_items()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new FakeProvider([new ImageResourceInfo("/media/photo.png"), new FileResourceInfo("/media/notes.txt")]);
        var query = new CatalogQuery(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        var filter = CatalogResourceFilter.Parse("contentType:image");

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["photo.png"], providerItems.Items.Select(resource => resource.Name));
    }

    [Fact]
    public async Task Should_report_running_only_while_the_fetch_is_in_flight()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var gate = new TaskCompletionSource();
        var provider = new FakeProvider([CreateResource("one")], gate);
        var query = new CatalogQuery(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        var changes = new List<bool>();

        // Act
        var fetch = providerItems.FetchAsync(provider, onStateChanged: () => changes.Add(providerItems.IsFetching), query, filter: null, append: false, TestContext.Current.CancellationToken);
        Assert.True(providerItems.IsFetching);
        gate.SetResult();
        await fetch;

        // Assert
        Assert.Multiple(
            () => Assert.False(providerItems.IsFetching),
            () => Assert.Equal([true, false], changes));
    }

    [Fact]
    public async Task Should_abandon_a_superseded_fetch_in_favor_of_the_newer_one()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var firstGate = new TaskCompletionSource();
        var firstProvider = new FakeProvider([CreateResource("stale")], firstGate);
        var secondProvider = new FakeProvider([CreateResource("fresh")]);
        var query = new CatalogQuery(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

        // Act
        var stale = providerItems.FetchAsync(firstProvider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);
        await providerItems.FetchAsync(secondProvider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);
        firstGate.SetResult();
        await stale;

        // Assert
        Assert.Equal(["fresh"], providerItems.Items.Select(resource => resource.Name));
    }

    [Fact]
    public async Task Should_abandon_a_fetch_when_the_caller_cancels()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        using var caller = new CancellationTokenSource();
        var gate = new TaskCompletionSource();
        var provider = new FakeProvider([CreateResource("late")], gate);
        var query = new CatalogQuery(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

        // Act
        var fetch = providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, caller.Token);
        await caller.CancelAsync();
        gate.SetResult();
        await fetch;

        // Assert
        Assert.Multiple(
            () => Assert.Empty(providerItems.Items),
            () => Assert.False(providerItems.IsFetching));
    }
}
