namespace Forma.Framework.Catalog.Tests.Queries;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

using Xunit;

public sealed class CatalogCollectionViewTests
{
    private static string GetTypeNameKindLabel(ICatalogItem item)
        => item.Resource?.GetType().Name ?? string.Empty;

    private static CatalogCollectionView CreateView(params ResourceInfo[] resources)
        => new(GetTypeNameKindLabel) { Source = CatalogItem.ForAll(resources), PageSize = 24 };

    private static FileResourceInfo CreateFile(string name, long? size = null, DateTimeOffset? createdAt = null, DateTimeOffset? modifiedAt = null)
        => new($"/media/{name}") { Size = size, CreatedAt = createdAt, ModifiedAt = modifiedAt };

    [Fact]
    public void Should_list_everything_in_name_order_by_default()
    {
        // Act
        var view = CreateView(CreateFile("cherry.png"), CreateFile("Apple.png"), CreateFile("banana.png"));

        // Assert
        Assert.Equal(["Apple.png", "banana.png", "cherry.png"], view.MatchingItems.Select(item => item.Name));
    }

    [Fact]
    public void Should_leave_hidden_items_out_and_keep_invisible_ones()
    {
        // Arrange
        var view = new CatalogCollectionView(GetTypeNameKindLabel)
        {
            Source =
            [
                CreateItem("shown.png", CatalogItemVisibility.Visible),
                CreateItem("hidden.png", CatalogItemVisibility.Hidden),
                CreateItem("invisible.png", CatalogItemVisibility.Invisible)
            ],
            PageSize = 24
        };

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["invisible.png", "shown.png"], view.MatchingItems.Select(item => item.Name)),
            () => Assert.Equal(2, view.MatchingCount));
    }

    private static CatalogItem CreateItem(string name, CatalogItemVisibility visibility)
        => new() { Key = name, Name = name, Visibility = visibility, Resource = CreateFile(name) };

    [Fact]
    public void Should_search_names_case_insensitively()
    {
        // Arrange
        var view = CreateView(CreateFile("Stage-One.jpg"), CreateFile("stage-two.jpg"), CreateFile("finish.jpg"));

        // Act
        view.SearchQuery = "STAGE";

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, view.MatchingCount),
            () => Assert.All(view.MatchingItems, item => Assert.Contains("tage", item.Name, StringComparison.Ordinal)));
    }

    [Fact]
    public void Should_keep_only_what_the_accept_filter_passes()
    {
        // Arrange
        var view = CreateView(
            new ImageResourceInfo("/media/photo.png"),
            new VideoResourceInfo("/media/clip.mp4"));

        // Act
        view.Filter = CatalogResourceFilter.Parse("contentType:image");

        // Assert
        Assert.Equal(["photo.png"], view.MatchingItems.Select(item => item.Name));
    }

    [Fact]
    public void Should_sort_by_each_field_in_both_directions()
    {
        // Arrange
        var older = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var small = CreateFile("small.txt", size: 10, createdAt: newer, modifiedAt: older);
        var large = CreateFile("large.txt", size: 900, createdAt: older, modifiedAt: newer);
        var view = CreateView(small, large);

        // Act & Assert
        view.SortField = CatalogSortField.Size;
        Assert.Equal([small, large], view.MatchingItems.Select(item => item.Resource));

        view.SortDirection = CatalogSortDirection.Descending;
        Assert.Equal([large, small], view.MatchingItems.Select(item => item.Resource));

        view.SortField = CatalogSortField.DateAdded;
        Assert.Equal([small, large], view.MatchingItems.Select(item => item.Resource));

        view.SortField = CatalogSortField.DateModified;
        Assert.Equal([large, small], view.MatchingItems.Select(item => item.Resource));
    }

    [Fact]
    public void Should_sort_by_the_kind_label_the_labeler_supplies()
    {
        // Arrange
        var video = new VideoResourceInfo("/media/clip.mp4");
        var image = new ImageResourceInfo("/media/photo.png");
        var view = CreateView(video, image);

        // Act
        view.SortField = CatalogSortField.Kind;

        // Assert
        Assert.Equal([image, video], view.MatchingItems.Select(item => item.Resource));
    }

    [Fact]
    public void Should_cut_the_matching_items_to_the_requested_page()
    {
        // Arrange
        var resources = Enumerable.Range(1, 25).Select(i => CreateFile($"file-{i:D2}.txt")).ToArray();
        var view = CreateView(resources);
        view.PageSize = 10;

        // Act
        view.PageNumber = 3;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(3, view.PageCount),
            () => Assert.Equal(5, view.VisibleItems.Count),
            () => Assert.Equal("file-21.txt", view.VisibleItems[0].Name),
            () => Assert.False(view.HasMorePages));
    }

    [Fact]
    public void Should_grow_the_window_from_the_start_while_appending()
    {
        // Arrange
        var resources = Enumerable.Range(1, 25).Select(i => CreateFile($"file-{i:D2}.txt")).ToArray();
        var view = CreateView(resources);
        view.PageSize = 10;
        view.IsAppending = true;

        // Act
        view.PageNumber = 2;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(20, view.VisibleItems.Count),
            () => Assert.Equal("file-01.txt", view.VisibleItems[0].Name),
            () => Assert.True(view.HasMorePages));
    }

    [Fact]
    public void Should_clamp_the_requested_page_to_the_pages_that_exist()
    {
        // Arrange
        var view = CreateView(CreateFile("one.txt"), CreateFile("two.txt"));
        view.PageSize = 10;

        // Act
        view.PageNumber = 99;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(1, view.EffectivePageNumber),
            () => Assert.Equal(2, view.VisibleItems.Count),
            () => Assert.False(view.HasMorePages));
    }

    [Fact]
    public void Should_reuse_the_shaped_lists_until_an_input_changes()
    {
        // Arrange
        var view = CreateView(CreateFile("one.txt"), CreateFile("two.txt"));

        // Act & Assert
        Assert.Multiple(
            () => Assert.Same(view.MatchingItems, view.MatchingItems),
            () => Assert.Same(view.VisibleItems, view.VisibleItems));

        var shapedBefore = view.MatchingItems;
        view.SearchQuery = "one";
        Assert.NotSame(shapedBefore, view.MatchingItems);
    }

    [Fact]
    public void Should_reshape_a_source_mutated_in_place_when_invalidated()
    {
        // Arrange
        var items = new List<ICatalogItem> { CatalogItem.For(CreateFile("one.txt")) };
        var view = new CatalogCollectionView(GetTypeNameKindLabel) { Source = items, PageSize = 24 };
        Assert.Equal(1, view.MatchingCount);

        // Act
        items.Add(CatalogItem.For(CreateFile("two.txt")));
        view.Invalidate();

        // Assert
        Assert.Equal(2, view.MatchingCount);
    }
}
