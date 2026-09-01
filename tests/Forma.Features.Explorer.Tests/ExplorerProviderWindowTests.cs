namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerProviderWindowTests
{
    private sealed class FakeProvider(IReadOnlyList<ResourceInfo> all, TaskCompletionSource? gate = null) : IExplorerResourcesProvider
    {
        private readonly IReadOnlyList<ResourceInfo> _all = all;
        private readonly TaskCompletionSource? _gate = gate;

        public int CallCount { get; private set; }

        public async ValueTask<ExplorerResourcePage> GetResourcesAsync(ExplorerResourceQuery query, CancellationToken cancellationToken)
        {
            CallCount++;

            if (_gate is not null)
            {
                await _gate.Task.WaitAsync(cancellationToken);
            }

            var page = _all.Skip(query.Skip).Take(query.Take).ToList();

            return new ExplorerResourcePage(page, _all.Count);
        }
    }

    private static ResourceInfo Resource(string name)
        => new($"/media/{name}");

    [Fact]
    public void Should_hold_nothing_before_the_first_fetch()
    {
        // Act
        using var window = new ExplorerProviderWindow();

        // Assert
        Assert.Multiple(
            () => Assert.Empty(window.Resources),
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
        var query = new ExplorerResourceQuery(0, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter: null, appendToWindow: false);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two"], window.Resources.Select(resource => resource.Name)),
            () => Assert.Equal(3, window.Total),
            () => Assert.True(window.WasFull));
    }

    [Fact]
    public async Task Should_extend_the_window_on_an_appending_fetch()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new FakeProvider([Resource("one"), Resource("two"), Resource("three")]);
        var firstPage = new ExplorerResourceQuery(0, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        var secondPage = new ExplorerResourceQuery(2, 2, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        await window.FetchAsync(provider, onStateChanged: () => { }, firstPage, filter: null, appendToWindow: false);

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, secondPage, filter: null, appendToWindow: true);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["one", "two", "three"], window.Resources.Select(resource => resource.Name)),
            () => Assert.False(window.WasFull));
    }

    [Fact]
    public async Task Should_keep_a_resource_the_filter_rejects_off_the_window()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var provider = new FakeProvider([new ImageResourceInfo("/media/photo.png"), new FileResourceInfo("/media/notes.txt")]);
        var query = new ExplorerResourceQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        var filter = ExplorerResourceFilter.Parse("contentType:image");

        // Act
        await window.FetchAsync(provider, onStateChanged: () => { }, query, filter, appendToWindow: false);

        // Assert
        Assert.Equal(["photo.png"], window.Resources.Select(resource => resource.Name));
    }

    [Fact]
    public async Task Should_report_running_only_while_the_fetch_is_in_flight()
    {
        // Arrange
        using var window = new ExplorerProviderWindow();
        var gate = new TaskCompletionSource();
        var provider = new FakeProvider([Resource("one")], gate);
        var query = new ExplorerResourceQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);
        var changes = new List<bool>();

        // Act
        var fetch = window.FetchAsync(provider, onStateChanged: () => changes.Add(window.IsFetching), query, filter: null, appendToWindow: false);
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
        var query = new ExplorerResourceQuery(0, 10, null, ExplorerSortField.Name, ExplorerSortDirection.Ascending);

        // Act
        var stale = window.FetchAsync(firstProvider, onStateChanged: () => { }, query, filter: null, appendToWindow: false);
        await window.FetchAsync(secondProvider, onStateChanged: () => { }, query, filter: null, appendToWindow: false);
        firstGate.SetResult();
        await stale;

        // Assert
        Assert.Equal(["fresh"], window.Resources.Select(resource => resource.Name));
    }
}
