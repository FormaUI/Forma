namespace Forma.Framework.Catalog.Tests.Queries;

using System.Globalization;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

using Xunit;

public sealed class CatalogListProviderTests
{
    private static CatalogListProvider CreateProvider(params ICatalogItem[] items)
        => new(items, item => item.GetKind().ToString(), () => CultureInfo.InvariantCulture);

    [Fact]
    public async Task Should_answer_the_slice_asked_for_of_the_items_it_lists_with_their_count()
    {
        // Arrange
        var provider = CreateProvider([.. CatalogItem.ForAll([.. Enumerable.Range(1, 5).Select(i => new ImageResourceInfo($"/media/{i}.png"))])]);

        // Act
        var page = await provider.GetItemsAsync(new CatalogQuery(null, 2, 2, null, CatalogSortField.Name, CatalogSortDirection.Ascending), TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["3.png", "4.png"], page.Items.Select(item => item.Name)),
            () => Assert.Equal(5, page.TotalCount),
            () => Assert.Equal(5, page.ListedCount));
    }

    [Fact]
    public async Task Should_leave_hidden_items_and_those_the_filter_rejects_out_before_slicing()
    {
        // Arrange
        var provider = CreateProvider(
            CatalogItem.For(new ImageResourceInfo("/media/a.png")),
            CatalogItem.For(new DocumentResourceInfo("/media/b.pdf")),
            new CatalogItem { Key = "hidden", Name = "c.png", Visibility = CatalogItemVisibility.Hidden, Resource = new ImageResourceInfo("/media/c.png") },
            CatalogItem.For(new ImageResourceInfo("/media/d.png")));
        var query = new CatalogQuery(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending) { Filter = CatalogResourceFilter.Parse("contentType:image") };

        // Act
        var page = await provider.GetItemsAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["a.png", "d.png"], page.Items.Select(item => item.Name)),
            () => Assert.Equal(2, page.TotalCount),
            () => Assert.Equal(2, page.ListedCount));
    }

    [Fact]
    public async Task Should_answer_the_group_sizes_only_when_asked()
    {
        // Arrange
        var provider = CreateProvider(CatalogItem.For(new ImageResourceInfo("/media/a.png")), CatalogItem.For(new VideoResourceInfo("/media/b.mp4")));
        var grouping = new CatalogGrouping(CatalogGroupField.Kind, [new CatalogKindGroup("Video", CatalogItemKind.Video), new CatalogKindGroup("Image", CatalogItemKind.Image)]);
        var query = new CatalogQuery(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending) { Grouping = grouping };

        // Act
        var unasked = await provider.GetItemsAsync(query, TestContext.Current.CancellationToken);
        var asked = await provider.GetItemsAsync(query with { IncludesGroupSizes = true }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["b.mp4", "a.png"], unasked.Items.Select(item => item.Name)),
            () => Assert.Null(unasked.GroupSizes),
            () => Assert.Equal([new CatalogGroupSize("Video", 1), new CatalogGroupSize("Image", 1)], asked.GroupSizes));
    }
}
