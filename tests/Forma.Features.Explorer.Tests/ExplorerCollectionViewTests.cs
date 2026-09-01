namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerCollectionViewTests
{
    private sealed class TypeNameKindLabels : IResourceKindLabels
    {
        public string LabelOf(ResourceInfo resource) => resource.GetType().Name;
    }

    private static ExplorerCollectionView CreateView(params ResourceInfo[] resources)
        => new(new TypeNameKindLabels()) { Source = resources, PageSize = 24 };

    private static FileResourceInfo File(string name, long? size = null, DateTimeOffset? createdAt = null, DateTimeOffset? modifiedAt = null)
        => new($"/media/{name}") { Size = size, CreatedAt = createdAt, ModifiedAt = modifiedAt };

    [Fact]
    public void Should_list_everything_in_name_order_by_default()
    {
        // Act
        var view = CreateView(File("cherry.png"), File("Apple.png"), File("banana.png"));

        // Assert
        Assert.Equal(["Apple.png", "banana.png", "cherry.png"], view.Matching.Select(resource => resource.Name));
    }

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
            () => Assert.All(view.Matching, resource => Assert.Contains("tage", resource.Name, StringComparison.Ordinal)));
    }

    [Fact]
    public void Should_keep_only_what_the_accept_filter_passes()
    {
        // Arrange
        var view = CreateView(
            new ImageResourceInfo("/media/photo.png"),
            new VideoResourceInfo("/media/clip.mp4"));

        // Act
        view.Accept = ExplorerResourceFilter.Parse("contentType:image");

        // Assert
        Assert.Equal(["photo.png"], view.Matching.Select(resource => resource.Name));
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
        Assert.Equal([small, large], view.Matching);

        view.SortDirection = ExplorerSortDirection.Descending;
        Assert.Equal([large, small], view.Matching);

        view.SortField = ExplorerSortField.DateAdded;
        Assert.Equal([small, large], view.Matching);

        view.SortField = ExplorerSortField.DateModified;
        Assert.Equal([large, small], view.Matching);
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
        Assert.Equal([image, video], view.Matching);
    }

    [Fact]
    public void Should_cut_the_matching_resources_to_the_requested_page()
    {
        // Arrange
        var resources = Enumerable.Range(1, 25).Select(i => File($"file-{i:D2}.txt")).ToArray();
        var view = CreateView(resources);
        view.PageSize = 10;

        // Act
        view.Page = 3;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(3, view.PageCount),
            () => Assert.Equal(5, view.Visible.Count),
            () => Assert.Equal("file-21.txt", view.Visible[0].Name),
            () => Assert.False(view.HasMore));
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
        view.Page = 2;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(20, view.Visible.Count),
            () => Assert.Equal("file-01.txt", view.Visible[0].Name),
            () => Assert.True(view.HasMore));
    }

    [Fact]
    public void Should_clamp_the_requested_page_to_the_pages_that_exist()
    {
        // Arrange
        var view = CreateView(File("one.txt"), File("two.txt"));
        view.PageSize = 10;

        // Act
        view.Page = 99;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(1, view.EffectivePage),
            () => Assert.Equal(2, view.Visible.Count),
            () => Assert.False(view.HasMore));
    }

    [Fact]
    public void Should_reuse_the_shaped_lists_until_an_input_changes()
    {
        // Arrange
        var view = CreateView(File("one.txt"), File("two.txt"));

        // Act & Assert
        Assert.Multiple(
            () => Assert.Same(view.Matching, view.Matching),
            () => Assert.Same(view.Visible, view.Visible));

        var shapedBefore = view.Matching;
        view.SearchQuery = "one";
        Assert.NotSame(shapedBefore, view.Matching);
    }

    [Fact]
    public void Should_reshape_a_source_mutated_in_place_when_invalidated()
    {
        // Arrange
        var resources = new List<ResourceInfo> { File("one.txt") };
        var view = new ExplorerCollectionView(new TypeNameKindLabels()) { Source = resources, PageSize = 24 };
        Assert.Equal(1, view.MatchingCount);

        // Act
        resources.Add(File("two.txt"));
        view.Invalidate();

        // Assert
        Assert.Equal(2, view.MatchingCount);
    }
}
