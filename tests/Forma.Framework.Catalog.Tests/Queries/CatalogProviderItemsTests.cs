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
        var query = new CatalogQuery(0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["shown"], providerItems.Items.Select(item => item.Name)),
            () => Assert.Equal(2, providerItems.FetchedCount),
            () => Assert.Equal(6, providerItems.Total),
            () => Assert.Equal(5, providerItems.ListedTotal));
    }

    [Fact]
    public async Task Should_count_what_it_fetched_and_left_out_across_appended_slices()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new ItemProvider([CatalogItem.For(CreateResource("shown")), CreateHiddenItem("hidden")], total: 6);
        var firstPage = new CatalogQuery(0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        var secondPage = new CatalogQuery(2, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, firstPage, filter: null, append: false, TestContext.Current.CancellationToken);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, secondPage, filter: null, append: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, providerItems.Items.Count),
            () => Assert.Equal(4, providerItems.FetchedCount),
            () => Assert.Equal(4, providerItems.ListedTotal),
            () => Assert.True(providerItems.WasFull));
    }

    [Fact]
    public async Task Should_start_the_counts_over_when_the_items_are_replaced()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new ItemProvider([CatalogItem.For(CreateResource("shown")), CreateHiddenItem("hidden")], total: 6);
        var query = new CatalogQuery(0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: true, TestContext.Current.CancellationToken);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, providerItems.FetchedCount),
            () => Assert.Equal(5, providerItems.ListedTotal));
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
            () => Assert.False(providerItems.WasFull),
            () => Assert.False(providerItems.IsFetching));
    }

    [Fact]
    public async Task Should_replace_the_items_on_a_non_appending_fetch()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new FakeProvider([CreateResource("one"), CreateResource("two"), CreateResource("three")]);
        var query = new CatalogQuery(0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, append: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two"], providerItems.Items.Select(resource => resource.Name)),
            () => Assert.Equal(3, providerItems.Total),
            () => Assert.True(providerItems.WasFull));
    }

    [Fact]
    public async Task Should_extend_the_items_on_an_appending_fetch()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new FakeProvider([CreateResource("one"), CreateResource("two"), CreateResource("three")]);
        var firstPage = new CatalogQuery(0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        var secondPage = new CatalogQuery(2, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, firstPage, filter: null, append: false, TestContext.Current.CancellationToken);

        // Act
        await providerItems.FetchAsync(provider, onStateChanged: () => { }, secondPage, filter: null, append: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two", "three"], providerItems.Items.Select(resource => resource.Name)),
            () => Assert.False(providerItems.WasFull));
    }

    [Fact]
    public async Task Should_keep_a_resource_the_filter_rejects_out_of_the_items()
    {
        // Arrange
        using var providerItems = new CatalogProviderItems();
        var provider = new FakeProvider([new ImageResourceInfo("/media/photo.png"), new FileResourceInfo("/media/notes.txt")]);
        var query = new CatalogQuery(0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
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
        var query = new CatalogQuery(0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);
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
        var query = new CatalogQuery(0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

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
        var query = new CatalogQuery(0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

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
