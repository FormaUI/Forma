namespace Forma.Framework.Catalog.Tests.Queries;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

using Xunit;

public sealed class CatalogProviderExtensionsTests
{
    private static readonly CatalogQuery Query = new(null, 0, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

    private static CatalogFolderItem CreateItem(string name)
        => CatalogItem.ForFolder(new FolderResourceInfo($"/media/{name}") { Name = name });

    // Answers each slice it is asked for with the next page it holds, recording where each slice started.
    private sealed class PagedProvider(params CatalogPage[] pages) : ICatalogProvider
    {
        public List<int> Skips { get; } = [];

        public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Skips.Add(query.Skip);

            return ValueTask.FromResult(Skips.Count <= pages.Length ? pages[Skips.Count - 1] : CatalogPage.Empty);
        }
    }

    public sealed class Reading
    {
        [Fact]
        public async Task Should_read_on_past_a_slice_that_holds_no_item_while_more_follow()
        {
            // Arrange - the middle slice read two positions and kept none
            var provider = new PagedProvider(
                new CatalogPage([CreateItem("a"), CreateItem("b")], null) { HasMore = true },
                new CatalogPage([], null) { ReadCount = 2, HasMore = true },
                new CatalogPage([CreateItem("e")], null) { ReadCount = 1, HasMore = false });

            // Act
            var items = await provider.ReadAllAsync(Query, TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["a", "b", "e"], items.Select(item => item.Name)),
                () => Assert.Equal([0, 2, 4], provider.Skips));
        }

        [Fact]
        public async Task Should_stop_where_the_positions_read_reach_the_total()
        {
            // Arrange
            var provider = new PagedProvider(
                new CatalogPage([CreateItem("a"), CreateItem("b")], 3),
                new CatalogPage([CreateItem("c")], 3),
                new CatalogPage([CreateItem("never")], 3));

            // Act
            var items = await provider.ReadAllAsync(Query, TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["a", "b", "c"], items.Select(item => item.Name)),
                () => Assert.Equal([0, 2], provider.Skips));
        }

        [Fact]
        public async Task Should_stop_at_a_slice_that_read_fewer_positions_than_asked_where_none_says_more()
        {
            // Arrange
            var provider = new PagedProvider(
                new CatalogPage([CreateItem("a"), CreateItem("b")], null),
                new CatalogPage([CreateItem("c")], null),
                new CatalogPage([CreateItem("never")], null));

            // Act
            var items = await provider.ReadAllAsync(Query, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(["a", "b", "c"], items.Select(item => item.Name));
        }

        [Fact]
        public async Task Should_stop_at_a_slice_that_read_nothing_though_it_says_more_follow()
        {
            // Arrange
            var provider = new PagedProvider(
                new CatalogPage([CreateItem("a"), CreateItem("b")], null) { HasMore = true },
                new CatalogPage([], null) { ReadCount = 0, HasMore = true },
                new CatalogPage([CreateItem("never")], null));

            // Act
            var items = await provider.ReadAllAsync(Query, TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["a", "b"], items.Select(item => item.Name)),
                () => Assert.Equal([0, 2], provider.Skips));
        }

        [Fact]
        public async Task Should_start_at_the_query_s_skip()
        {
            // Arrange
            var provider = new PagedProvider(new CatalogPage([CreateItem("c")], 3));

            // Act
            await provider.ReadAllAsync(Query with { Skip = 2 }, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal([2], provider.Skips);
        }

        [Fact]
        public async Task Should_carry_the_query_to_every_slice()
        {
            // Arrange
            var kinds = new HashSet<CatalogItemKind> { CatalogItemKind.Folder };
            List<CatalogQuery> asked = [];
            var provider = new RecordingProvider(asked);

            // Act
            await provider.ReadAllAsync(Query with { Kinds = kinds, Search = "a" }, TestContext.Current.CancellationToken);

            // Assert
            Assert.All(asked, query => Assert.Equal((kinds, "a", 2), (query.Kinds, query.Search, query.Take)));
        }

        [Fact]
        public async Task Should_stop_once_cancelled()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();
            var provider = new PagedProvider(new CatalogPage([CreateItem("a")], 1));

            // Act and assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.ReadAllAsync(Query, cancellation.Token));
        }

        private sealed class RecordingProvider(List<CatalogQuery> asked) : ICatalogProvider
        {
            public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
            {
                asked.Add(query);

                return ValueTask.FromResult(new CatalogPage(asked.Count == 1 ? [CreateItem("a"), CreateItem("b")] : [CreateItem("c")], 3));
            }
        }
    }
}
