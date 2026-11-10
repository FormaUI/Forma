namespace Forma.Framework.Explorer.Tests.Content;

using System.Globalization;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerContentProviderTests
{
    // Records each query it is asked, answering from the files it holds.
    private sealed class RecordingFiles(IReadOnlyList<ICatalogItem> items) : ICatalogProvider
    {
        private readonly CatalogListProvider _list = new(items, item => item.GetKind().ToString(), () => CultureInfo.InvariantCulture);

        public List<CatalogQuery> Queries { get; } = [];

        public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
        {
            Queries.Add(query);

            return _list.GetItemsAsync(query, cancellationToken);
        }
    }

    private static IReadOnlyList<ICatalogItem> CreateFolders(int count, string? parent = null)
        => CatalogItem.ForAll([.. Enumerable.Range(1, count).Select(i => new FolderResourceInfo($"folder-{parent}{i:D2}") { Name = $"Folder {i:D2}", Parent = parent })]);

    private static IReadOnlyList<ICatalogItem> CreateFiles(int count)
        => CatalogItem.ForAll([.. Enumerable.Range(1, count).Select(i => new ImageResourceInfo($"/media/file-{i:D2}.png"))]);

    private static CatalogQuery CreateQuery(int skip, int take, string? folderKey = null, string? search = null, CatalogSortDirection direction = CatalogSortDirection.Ascending)
        => new(folderKey, skip, take, search, CatalogSortField.Name, direction);

    private static async Task<CatalogPage> GetAsync(ICatalogProvider provider, CatalogQuery query)
        => await provider.GetItemsAsync(query, TestContext.Current.CancellationToken);

    public sealed class Boundary
    {
        [Fact]
        public async Task Should_end_a_page_on_folders_and_start_the_files_at_their_first_position_on_the_next()
        {
            // Arrange - 23 folders and pages of 20
            var files = new RecordingFiles(CreateFiles(30));
            var provider = new ExplorerContentProvider(new CatalogFolderTree(CreateFolders(23)), files);

            // Act
            var first = await GetAsync(provider, CreateQuery(0, 20));
            var askedForFirst = files.Queries.Count;
            var second = await GetAsync(provider, CreateQuery(20, 20));
            var third = await GetAsync(provider, CreateQuery(40, 20));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(20, first.Items.Count),
                () => Assert.All(first.Items, item => Assert.IsAssignableFrom<ICatalogFolderItem>(item)),
                () => Assert.Equal(0, askedForFirst),
                () => Assert.Equal(["Folder 21", "Folder 22", "Folder 23", "file-01.png"], second.Items.Take(4).Select(item => item.Name)),
                () => Assert.Equal(20, second.Items.Count),
                () => Assert.Equal((0, 17), (files.Queries[0].Skip, files.Queries[0].Take)),
                () => Assert.Equal((17, 20), (files.Queries[1].Skip, files.Queries[1].Take)),
                () => Assert.Equal(["file-18.png", "file-19.png", "file-20.png"], third.Items.Take(3).Select(item => item.Name)),
                () => Assert.Equal(53, second.TotalCount));
        }

        [Fact]
        public async Task Should_read_no_files_for_a_page_exactly_filled_by_folders()
        {
            // Arrange
            var files = new RecordingFiles(CreateFiles(5));
            var provider = new ExplorerContentProvider(new CatalogFolderTree(CreateFolders(20)), files);

            // Act
            var first = await GetAsync(provider, CreateQuery(0, 20));
            var second = await GetAsync(provider, CreateQuery(20, 20));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(20, first.Items.Count),
                () => Assert.True(first.HasMore),
                () => Assert.Null(first.TotalCount),
                () => Assert.Equal(["file-01.png", "file-02.png", "file-03.png", "file-04.png", "file-05.png"], second.Items.Select(item => item.Name)),
                () => Assert.Equal((0, 20), (Assert.Single(files.Queries).Skip, files.Queries[0].Take)));
        }

        [Fact]
        public async Task Should_lead_a_page_with_fewer_folders_than_it_holds_and_fill_it_with_files()
        {
            // Arrange
            var files = new RecordingFiles(CreateFiles(30));
            var provider = new ExplorerContentProvider(new CatalogFolderTree(CreateFolders(5)), files);

            // Act
            var first = await GetAsync(provider, CreateQuery(0, 20));
            await GetAsync(provider, CreateQuery(20, 20));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(5, first.Items.OfType<ICatalogFolderItem>().Count()),
                () => Assert.Equal(20, first.Items.Count),
                () => Assert.Equal(20, first.ReadCount),
                () => Assert.Equal([(0, 15), (15, 20)], files.Queries.Select(query => (query.Skip, query.Take))));
        }

        [Fact]
        public async Task Should_move_the_files_offset_by_the_folders_a_search_matches()
        {
            // Arrange - of five folders only Folder 01 holds a 1, as twelve of thirty files do
            var files = new RecordingFiles(CreateFiles(30));
            var provider = new ExplorerContentProvider(new CatalogFolderTree(CreateFolders(5)), files);

            // Act - the second page of four
            var everything = await GetAsync(provider, CreateQuery(4, 4));
            var searched = await GetAsync(provider, CreateQuery(4, 4, search: "1"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["Folder 05", "file-01.png", "file-02.png", "file-03.png"], everything.Items.Select(item => item.Name)),
                () => Assert.Equal((0, 3), (files.Queries[0].Skip, files.Queries[0].Take)),
                () => Assert.Equal(["file-12.png", "file-13.png", "file-14.png", "file-15.png"], searched.Items.Select(item => item.Name)),
                () => Assert.Equal((3, 4), (files.Queries[1].Skip, files.Queries[1].Take)),
                () => Assert.Equal(13, searched.TotalCount));
        }

        [Fact]
        public async Task Should_keep_the_folders_first_in_a_descending_sort()
        {
            // Arrange
            var files = new RecordingFiles(CreateFiles(3));
            var provider = new ExplorerContentProvider(new CatalogFolderTree(CreateFolders(2)), files);

            // Act
            var page = await GetAsync(provider, CreateQuery(0, 10, direction: CatalogSortDirection.Descending));

            // Assert
            Assert.Equal(["Folder 02", "Folder 01", "file-03.png", "file-02.png", "file-01.png"], page.Items.Select(item => item.Name));
        }
    }

    public sealed class Location
    {
        [Fact]
        public async Task Should_lead_every_folders_files_with_the_top_level_folders_and_a_folder_with_its_own_children()
        {
            // Arrange
            IReadOnlyList<ICatalogItem> folders = [.. CreateFolders(2), .. CreateFolders(1, parent: "folder-01")];
            var files = new RecordingFiles(CreateFiles(1));
            var provider = new ExplorerContentProvider(new CatalogFolderTree(folders), files);

            // Act
            var everything = await GetAsync(provider, CreateQuery(0, 10));
            var inside = await GetAsync(provider, CreateQuery(0, 10, folderKey: "folder-01"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["Folder 01", "Folder 02", "file-01.png"], everything.Items.Select(item => item.Name)),
                () => Assert.Equal(["folder-folder-0101", "/media/file-01.png"], inside.Items.Select(item => item.Key)),
                () => Assert.Equal([null, "folder-01"], files.Queries.Select(query => query.FolderKey)));
        }

        [Fact]
        public async Task Should_leave_a_hidden_folder_out_of_the_items_and_the_count()
        {
            // Arrange
            IReadOnlyList<ICatalogItem> folders =
            [
                .. CreateFolders(1),
                new CatalogFolderItem { Key = "hidden", Name = "Hidden", Visibility = CatalogItemVisibility.Hidden },
            ];
            var provider = new ExplorerContentProvider(new CatalogFolderTree(folders), new RecordingFiles(CreateFiles(1)));

            // Act
            var page = await GetAsync(provider, CreateQuery(0, 10));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["Folder 01", "file-01.png"], page.Items.Select(item => item.Name)),
                () => Assert.Equal(2, page.TotalCount),
                () => Assert.Equal(2, page.ListedCount));
        }
    }

    public sealed class Grouping
    {
        private static CatalogGrouping CreateNameGrouping(CatalogSortDirection direction = CatalogSortDirection.Ascending)
            => ExplorerGrouping.Define(ExplorerGroupField.Name, direction, kind => kind.ToString(), CultureInfo.InvariantCulture, DateTimeOffset.UnixEpoch, TimeZoneInfo.Utc, DayOfWeek.Monday)!;

        [Theory]
        [InlineData(CatalogSortDirection.Ascending)]
        [InlineData(CatalogSortDirection.Descending)]
        public async Task Should_gather_the_folders_in_one_leading_group_whatever_their_names(CatalogSortDirection direction)
        {
            // Arrange
            var provider = new ExplorerContentProvider(new CatalogFolderTree(CreateFolders(2)), new RecordingFiles(CreateFiles(3)));
            var grouping = CreateNameGrouping(direction);

            // Act
            var page = await GetAsync(provider, CreateQuery(0, 10, direction: direction) with { Grouping = grouping, IncludesGroupSizes = true });

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Kind.Folder", grouping.Groups[0].Key),
                () => Assert.Equal([new CatalogGroupSize("Kind.Folder", 2), new CatalogGroupSize("NameAToH", 3)], page.GroupSizes),
                () => Assert.All(page.Items.Take(2), item => Assert.Equal(0, grouping.IndexOf(item))));
        }

        [Fact]
        public async Task Should_report_the_files_counts_on_a_page_of_folders_alone_once_its_sequence_read_them()
        {
            // Arrange - three folders and pages of two
            var provider = new ExplorerContentProvider(new CatalogFolderTree(CreateFolders(3)), new RecordingFiles(CreateFiles(4)));
            var grouping = CreateNameGrouping();
            await GetAsync(provider, CreateQuery(2, 2) with { Grouping = grouping, IncludesGroupSizes = true });

            // Act
            var first = await GetAsync(provider, CreateQuery(0, 2) with { Grouping = grouping, IncludesGroupSizes = true });

            // Assert
            Assert.Multiple(
                () => Assert.Equal(7, first.TotalCount),
                () => Assert.Equal(7, first.ListedCount),
                () => Assert.True(first.HasMore),
                () => Assert.Equal([new CatalogGroupSize("Kind.Folder", 3), new CatalogGroupSize("NameAToH", 4)], first.GroupSizes));
        }
    }
}
