namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerCollectionViewTests
{
    private static string TypeNameKindLabel(IExplorerItem item)
        => item.Resource?.GetType().Name ?? string.Empty;

    private static ExplorerCollectionView CreateView(params ResourceInfo[] resources)
        => new(TypeNameKindLabel) { Source = ExplorerItem.ForAll(resources), PageSize = 24 };

    private static FileResourceInfo File(string name, long? size = null, DateTimeOffset? createdAt = null, DateTimeOffset? modifiedAt = null)
        => new($"/media/{name}") { Size = size, CreatedAt = createdAt, ModifiedAt = modifiedAt };

    [Fact]
    public void Should_list_everything_in_name_order_by_default()
    {
        // Act
        var view = CreateView(File("cherry.png"), File("Apple.png"), File("banana.png"));

        // Assert
        Assert.Equal(["Apple.png", "banana.png", "cherry.png"], view.MatchingItems.Select(item => item.Name));
    }

    [Fact]
    public void Should_leave_hidden_items_out_and_keep_invisible_ones()
    {
        // Arrange
        var view = new ExplorerCollectionView(TypeNameKindLabel)
        {
            Source =
            [
                Item("shown.png", ExplorerItemVisibility.Visible),
                Item("hidden.png", ExplorerItemVisibility.Hidden),
                Item("invisible.png", ExplorerItemVisibility.Invisible)
            ],
            PageSize = 24
        };

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["invisible.png", "shown.png"], view.MatchingItems.Select(item => item.Name)),
            () => Assert.Equal(2, view.MatchingCount));
    }

    private static ExplorerItem Item(string name, ExplorerItemVisibility visibility)
        => new() { Key = name, Name = name, Visibility = visibility, Resource = File(name) };

    [Fact]
    public void Should_search_names_case_insensitively()
    {
        // Arrange
        var view = CreateView(File("Stage-One.jpg"), File("stage-two.jpg"), File("finish.jpg"));

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
        view.Filter = ExplorerResourceFilter.Parse("contentType:image");

        // Assert
        Assert.Equal(["photo.png"], view.MatchingItems.Select(item => item.Name));
    }

    [Fact]
    public void Should_sort_by_each_field_in_both_directions()
    {
        // Arrange
        var older = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var small = File("small.txt", size: 10, createdAt: newer, modifiedAt: older);
        var large = File("large.txt", size: 900, createdAt: older, modifiedAt: newer);
        var view = CreateView(small, large);

        // Act & Assert
        view.SortField = ExplorerSortField.Size;
        Assert.Equal([small, large], view.MatchingItems.Select(item => item.Resource));

        view.SortDirection = ExplorerSortDirection.Descending;
        Assert.Equal([large, small], view.MatchingItems.Select(item => item.Resource));

        view.SortField = ExplorerSortField.DateAdded;
        Assert.Equal([small, large], view.MatchingItems.Select(item => item.Resource));

        view.SortField = ExplorerSortField.DateModified;
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
        view.SortField = ExplorerSortField.Kind;

        // Assert
        Assert.Equal([image, video], view.MatchingItems.Select(item => item.Resource));
    }

    [Fact]
    public void Should_cut_the_matching_items_to_the_requested_page()
    {
        // Arrange
        var resources = Enumerable.Range(1, 25).Select(i => File($"file-{i:D2}.txt")).ToArray();
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
        var resources = Enumerable.Range(1, 25).Select(i => File($"file-{i:D2}.txt")).ToArray();
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
        var view = CreateView(File("one.txt"), File("two.txt"));
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
        var view = CreateView(File("one.txt"), File("two.txt"));

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
        var items = new List<IExplorerItem> { ExplorerItem.For(File("one.txt")) };
        var view = new ExplorerCollectionView(TypeNameKindLabel) { Source = items, PageSize = 24 };
        Assert.Equal(1, view.MatchingCount);

        // Act
        items.Add(ExplorerItem.For(File("two.txt")));
        view.Invalidate();

        // Assert
        Assert.Equal(2, view.MatchingCount);
    }
}
