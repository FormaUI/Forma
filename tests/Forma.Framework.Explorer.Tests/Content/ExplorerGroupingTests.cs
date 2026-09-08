namespace Forma.Framework.Explorer.Tests.Content;

using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerGroupingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    private static string GetTypeNameKindLabel(IExplorerItem item)
        => item.Resource?.GetType().Name ?? string.Empty;

    private static FileResourceInfo CreateFile(string name, long? size = null, DateTimeOffset? modifiedAt = null)
        => new($"/media/{name}") { Size = size, ModifiedAt = modifiedAt };

    private static IReadOnlyList<ExplorerItemGroup> Group(ExplorerGroupField field, ExplorerSortDirection direction = ExplorerSortDirection.Ascending, params ResourceInfo[] resources)
        => ExplorerGrouping.GroupBy(ExplorerItem.ForAll(resources), field, direction, GetTypeNameKindLabel, Now, DayOfWeek.Monday);

    [Fact]
    public void Should_yield_no_groups_when_nothing_groups_the_items()
    {
        // Act
        var groups = Group(ExplorerGroupField.None, resources: [CreateFile("a.png")]);

        // Assert
        Assert.Empty(groups);
    }

    [Fact]
    public void Should_band_names_by_their_first_character_in_band_order()
    {
        // Act
        var groups = Group(ExplorerGroupField.Name, resources: [CreateFile("zebra.png"), CreateFile("apple.png"), CreateFile("7up.png"), CreateFile("_notes.md"), CreateFile("kite.png")]);

        // Assert
        Assert.Equal(
            [ExplorerGroupBand.NameDigits, ExplorerGroupBand.NameAToH, ExplorerGroupBand.NameIToP, ExplorerGroupBand.NameQToZ, ExplorerGroupBand.NameOther],
            groups.Select(group => group.Band));
    }

    [Fact]
    public void Should_keep_each_groups_items_in_list_order()
    {
        // Act
        var groups = Group(ExplorerGroupField.Name, resources: [CreateFile("banana.png"), CreateFile("apple.png"), CreateFile("cherry.png")]);

        // Assert
        Assert.Equal(["banana.png", "apple.png", "cherry.png"], Assert.Single(groups).Items.Select(item => item.Name));
    }

    [Theory]
    [InlineData(null, ExplorerGroupBand.SizeUnspecified)]
    [InlineData(0L, ExplorerGroupBand.SizeEmpty)]
    [InlineData(16 * 1024L, ExplorerGroupBand.SizeTiny)]
    [InlineData(16 * 1024L + 1, ExplorerGroupBand.SizeSmall)]
    [InlineData(1024 * 1024L, ExplorerGroupBand.SizeSmall)]
    [InlineData(2 * 1024 * 1024L, ExplorerGroupBand.SizeMedium)]
    [InlineData(200 * 1024 * 1024L, ExplorerGroupBand.SizeLarge)]
    [InlineData(2L * 1024 * 1024 * 1024, ExplorerGroupBand.SizeHuge)]
    [InlineData(5L * 1024 * 1024 * 1024, ExplorerGroupBand.SizeGigantic)]
    public void Should_band_sizes_at_the_conventional_thresholds(long? size, ExplorerGroupBand expected)
    {
        // Act
        var groups = Group(ExplorerGroupField.Size, resources: [CreateFile("a.bin", size)]);

        // Assert
        Assert.Equal(expected, Assert.Single(groups).Band);
    }

    [Theory]
    [InlineData(0, ExplorerGroupBand.DateToday)]
    [InlineData(1, ExplorerGroupBand.DateYesterday)]
    [InlineData(2, ExplorerGroupBand.DateEarlierThisWeek)]
    [InlineData(5, ExplorerGroupBand.DateLastWeek)]
    [InlineData(10, ExplorerGroupBand.DateEarlierThisMonth)]
    [InlineData(20, ExplorerGroupBand.DateLastMonth)]
    [InlineData(100, ExplorerGroupBand.DateEarlierThisYear)]
    [InlineData(400, ExplorerGroupBand.DateLongAgo)]
    public void Should_band_dates_against_today_and_the_week_start(int daysAgo, ExplorerGroupBand expected)
    {
        // Arrange - the 16th of September 2026 is a Wednesday, so the week began on the 14th
        var groups = Group(ExplorerGroupField.DateModified, resources: [CreateFile("a.png", modifiedAt: Now.AddDays(-daysAgo))]);

        // Assert
        Assert.Equal(expected, Assert.Single(groups).Band);
    }

    [Fact]
    public void Should_band_an_undated_item_apart()
    {
        // Act
        var groups = Group(ExplorerGroupField.DateAdded, resources: [CreateFile("a.png")]);

        // Assert
        Assert.Equal(ExplorerGroupBand.DateUnspecified, Assert.Single(groups).Band);
    }

    [Fact]
    public void Should_name_kind_groups_and_order_them_by_name()
    {
        // Act
        var groups = Group(ExplorerGroupField.Kind, resources: [new ImageResourceInfo("https://example.com/a.jpg"), CreateFile("b.txt"), new ImageResourceInfo("https://example.com/c.jpg")]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["FileResourceInfo", "ImageResourceInfo"], groups.Select(group => group.KindLabel)),
            () => Assert.Equal(2, groups[1].Items.Count),
            () => Assert.Equal("kind:ImageResourceInfo", groups[1].Key));
    }

    [Fact]
    public void Should_reverse_the_groups_for_a_descending_direction()
    {
        // Act
        var groups = Group(ExplorerGroupField.Name, ExplorerSortDirection.Descending, CreateFile("apple.png"), CreateFile("zebra.png"));

        // Assert
        Assert.Equal([ExplorerGroupBand.NameQToZ, ExplorerGroupBand.NameAToH], groups.Select(group => group.Band));
    }
}
