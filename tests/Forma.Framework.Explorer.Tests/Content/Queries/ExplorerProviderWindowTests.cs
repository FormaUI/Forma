namespace Forma.Framework.Explorer.Tests.Content.Queries;

using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerProviderWindowTests
{
    private sealed class FakeProvider(IReadOnlyList<ResourceInfo> all, TaskCompletionSource? gate = null) : IExplorerItemsProvider
    {
        private readonly IReadOnlyList<ResourceInfo> _all = all;
        private readonly TaskCompletionSource? _gate = gate;

        public int CallCount { get; private set; }

        public async ValueTask<ExplorerItemPage> GetItemsAsync(ExplorerItemQuery query, CancellationToken cancellationToken)
        {
            CallCount++;

            if (_gate is not null)
            {
                await _gate.Task.WaitAsync(cancellationToken);
            }

            return new ExplorerItemPage(ExplorerItem.ForAll(_all.Skip(query.Skip).Take(query.Take)), _all.Count);
        }
    }

    private sealed class ItemProvider(IReadOnlyList<IExplorerItem> items, int? total = null) : IExplorerItemsProvider
    {
        public ValueTask<ExplorerItemPage> GetItemsAsync(ExplorerItemQuery query, CancellationToken cancellationToken)
            => ValueTask.FromResult(new ExplorerItemPage(items, total ?? items.Count));
    }

    private static ExplorerItem Hidden(string name)
        => new() { Key = name, Name = name, Visibility = ExplorerItemVisibility.Hidden };

    private static ResourceInfo Resource(string name)
        => new($"/media/{name}");

    [Fact]
    public async Task Should_leave_hidden_items_out_of_the_window()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new ItemProvider([ExplorerItem.For(Resource("shown")), Hidden("hidden")], total: 6);
        var query = new ExplorerItemQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["shown"], window.Items.Select(item => item.Name)),
            () => Assert.Equal(2, window.FetchedCount),
            () => Assert.Equal(6, window.Total),
            () => Assert.Equal(5, window.ListedTotal));
    }

    [Fact]
    public async Task Should_count_what_it_fetched_and_left_out_across_appended_windows()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new ItemProvider([ExplorerItem.For(Resource("shown")), Hidden("hidden")], total: 6);
        var firstPage = new ExplorerItemQuery(0, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        var secondPage = new ExplorerItemQuery(2, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        await window.FetchAsync(provider, onStateChanged: () => { }, firstPage, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, secondPage, filter: null, appendToWindow: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, window.Items.Count),
            () => Assert.Equal(4, window.FetchedCount),
            () => Assert.Equal(4, window.ListedTotal),
            () => Assert.True(window.WasFull));
    }

    [Fact]
    public async Task Should_start_the_counts_over_when_the_window_is_replaced()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new ItemProvider([ExplorerItem.For(Resource("shown")), Hidden("hidden")], total: 6);
        var query = new ExplorerItemQuery(0, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, appendToWindow: true, TestContext.Current.CancellationToken);

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, window.FetchedCount),
            () => Assert.Equal(5, window.ListedTotal));
    }

    [Fact]
    public void Should_hold_nothing_before_the_first_fetch()
    {
        // Act
        using var window = new ExplorerProviderWindow();

        // Assert
        Assert.Multiple(
            () => Assert.Empty(window.Items),
            () => Assert.Null(window.Total),
            () => Assert.False(window.WasFull),
            () => Assert.False(window.IsFetching));
    }

    [Fact]
    public async Task Should_replace_the_window_on_a_non_appending_fetch()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new FakeProvider([Resource("one"), Resource("two"), Resource("three")]);
        var query = new ExplorerItemQuery(0, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two"], window.Items.Select(resource => resource.Name)),
            () => Assert.Equal(3, window.Total),
            () => Assert.True(window.WasFull));
    }

    [Fact]
    public async Task Should_extend_the_window_on_an_appending_fetch()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new FakeProvider([Resource("one"), Resource("two"), Resource("three")]);
        var firstPage = new ExplorerItemQuery(0, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        var secondPage = new ExplorerItemQuery(2, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        await window.FetchAsync(provider, onStateChanged: () => { }, firstPage, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, secondPage, filter: null, appendToWindow: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two", "three"], window.Items.Select(resource => resource.Name)),
            () => Assert.False(window.WasFull));
    }

    [Fact]
    public async Task Should_keep_a_resource_the_filter_rejects_off_the_window()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new FakeProvider([new ImageResourceInfo("/media/photo.png"), new FileResourceInfo("/media/notes.txt")]);
        var query = new ExplorerItemQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        var filter = ExplorerResourceFilter.Parse("contentType:image");

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter, appendToWindow: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["photo.png"], window.Items.Select(resource => resource.Name));
    }

    [Fact]
    public async Task Should_report_running_only_while_the_fetch_is_in_flight()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var gate = new TaskCompletionSource();
        var provider = new FakeProvider([Resource("one")], gate);
        var query = new ExplorerItemQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        var changes = new List<bool>();

        // Act
        var fetch = window.FetchAsync(provider, onStateChanged: () => changes.Add(window.IsFetching), query, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);
        Assert.True(window.IsFetching);
        gate.SetResult();
        await fetch;

        // Assert
        Assert.Multiple(
            () => Assert.False(window.IsFetching),
            () => Assert.Equal([true, false], changes));
    }

    [Fact]
    public async Task Should_abandon_a_superseded_fetch_in_favor_of_the_newer_one()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var firstGate = new TaskCompletionSource();
        var firstProvider = new FakeProvider([Resource("stale")], firstGate);
        var secondProvider = new FakeProvider([Resource("fresh")]);
        var query = new ExplorerItemQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);

        // Act
        var stale = window.FetchAsync(firstProvider, onStateChanged: () => { }, query, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);
        await window.FetchAsync(secondProvider, onStateChanged: () => { }, query, filter: null, appendToWindow: false, TestContext.Current.CancellationToken);
        firstGate.SetResult();
        await stale;

        // Assert
        Assert.Equal(["fresh"], window.Items.Select(resource => resource.Name));
    }

    [Fact]
    public async Task Should_abandon_a_fetch_when_the_caller_cancels()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        using var caller = new CancellationTokenSource();
        var gate = new TaskCompletionSource();
        var provider = new FakeProvider([Resource("late")], gate);
        var query = new ExplorerItemQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);

        // Act
        var fetch = window.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, appendToWindow: false, caller.Token);
        await caller.CancelAsync();
        gate.SetResult();
        await fetch;

        // Assert
        Assert.Multiple(
            () => Assert.Empty(window.Items),
            () => Assert.False(window.IsFetching));
    }
}
