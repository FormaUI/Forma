namespace Forma.Framework.Explorer.Tests.Content;

using System.Globalization;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerGroupingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    private static string GetKindName(CatalogItemKind kind)
        => kind.ToString();

    private static FileResourceInfo CreateFile(string name, long? size = null, DateTimeOffset? modifiedAt = null)
        => new($"/media/{name}") { Size = size, ModifiedAt = modifiedAt };

    private static CatalogGrouping? Define(ExplorerGroupField field, CatalogSortDirection direction = CatalogSortDirection.Ascending, TimeZoneInfo? timeZone = null)
        => ExplorerGrouping.Define(field, direction, GetKindName, CultureInfo.InvariantCulture, Now, timeZone ?? TimeZoneInfo.Utc, DayOfWeek.Monday);

    // A group the definition gathers items into, as the data view gathers them: its key, its band, a kind's label, and its items.
    private sealed record Gathered(string Key, ExplorerGroupBand Band, string? KindLabel, IReadOnlyList<ICatalogItem> Items);

    // Gathers the items into the definition's groups that hold any, in the order the groups stand, each keeping them in list order.
    private static List<Gathered> Gather(IReadOnlyList<ICatalogItem> items, CatalogGrouping? grouping)
    {
        if (grouping is null)
        {
            return [];
        }

        return
        [
            .. items.GroupBy(grouping.IndexOf).OrderBy(group => group.Key).Select(group =>
            {
                var definition = grouping.Groups[group.Key];
                var (band, label) = definition is CatalogKindGroup kind
                    ? (ExplorerGroupBand.Kind, GetKindName(kind.Kind))
                    : (Enum.Parse<ExplorerGroupBand>(definition.Key), null);

                return new Gathered(definition.Key, band, label, [.. group]);
            }),
        ];
    }

    private static List<Gathered> Group(ExplorerGroupField field, CatalogSortDirection direction = CatalogSortDirection.Ascending, params ResourceInfo[] resources)
        => Gather(CatalogItem.ForAll(resources), Define(field, direction));

    [Fact]
    public void Should_yield_no_groups_when_nothing_groups_the_items()
    {
        // Act
        var groups = Group(ExplorerGroupField.None, resources: [CreateFile("a.png")]);

        // Assert
        Assert.Multiple(
            () => Assert.Null(Define(ExplorerGroupField.None)),
            () => Assert.Empty(groups));
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
            () => Assert.Equal(["File", "Image"], groups.Select(group => group.KindLabel)),
            () => Assert.Equal(2, groups[1].Items.Count),
            () => Assert.Equal("Kind.Image", groups[1].Key));
    }

    [Fact]
    public void Should_key_a_kind_group_by_its_kind_whatever_its_label()
    {
        // Act
        var english = ExplorerGrouping.Define(ExplorerGroupField.Kind, CatalogSortDirection.Ascending, kind => kind.ToString(), CultureInfo.InvariantCulture, Now, TimeZoneInfo.Utc, DayOfWeek.Monday);
        var reversed = ExplorerGrouping.Define(ExplorerGroupField.Kind, CatalogSortDirection.Ascending, kind => new string([.. kind.ToString().Reverse()]), CultureInfo.InvariantCulture, Now, TimeZoneInfo.Utc, DayOfWeek.Monday);

        // Assert — the labels order the groups, and the keys stay the kinds'
        Assert.Multiple(
            () => Assert.Equal(english!.Groups.Select(group => group.Key).Order(StringComparer.Ordinal), reversed!.Groups.Select(group => group.Key).Order(StringComparer.Ordinal)),
            () => Assert.NotEqual(english!.Groups.Select(group => group.Key), reversed!.Groups.Select(group => group.Key)));
    }

    [Fact]
    public void Should_bound_the_date_bands_by_the_instants_the_time_zones_days_begin()
    {
        // Arrange — noon on the 16th in UTC is ten at night there, ten hours ahead
        var zone = TimeZoneInfo.CreateCustomTimeZone("Ten Ahead", TimeSpan.FromHours(10), "Ten Ahead", "Ten Ahead");

        // Act
        var grouping = Define(ExplorerGroupField.DateModified, timeZone: zone)!;
        var today = (CatalogDateGroup)grouping.Groups.Single(group => group.Key == nameof(ExplorerGroupBand.DateToday));
        var yesterday = (CatalogDateGroup)grouping.Groups.Single(group => group.Key == nameof(ExplorerGroupBand.DateYesterday));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new DateTimeOffset(2026, 9, 16, 0, 0, 0, TimeSpan.FromHours(10)), today.From),
            () => Assert.Null(today.Before),
            () => Assert.Equal(new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.FromHours(10)), yesterday.From),
            () => Assert.Equal(today.From, yesterday.Before));
    }

    [Fact]
    public void Should_band_a_date_by_the_day_it_falls_on_in_the_time_zone()
    {
        // Arrange — an hour before noon UTC is today in UTC, and nine at night there, ten hours ahead
        var zone = TimeZoneInfo.CreateCustomTimeZone("Ten Ahead", TimeSpan.FromHours(10), "Ten Ahead", "Ten Ahead");
        var items = CatalogItem.ForAll([CreateFile("late.png", modifiedAt: new DateTimeOffset(2026, 9, 15, 15, 0, 0, TimeSpan.Zero))]);

        // Act — the 15th at three in the afternoon UTC is the 16th at one in the morning ten hours ahead
        var utc = Gather(items, Define(ExplorerGroupField.DateModified, timeZone: TimeZoneInfo.Utc));
        var ahead = Gather(items, Define(ExplorerGroupField.DateModified, timeZone: zone));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ExplorerGroupBand.DateYesterday, Assert.Single(utc).Band),
            () => Assert.Equal(ExplorerGroupBand.DateToday, Assert.Single(ahead).Band));
    }

    [Fact]
    public void Should_leave_a_band_empty_whose_days_a_newer_band_holds()
    {
        // Act — the 3rd of September 2026 is a Thursday, so last week began in August, before the month did
        var grouping = ExplorerGrouping.Define(ExplorerGroupField.DateModified, CatalogSortDirection.Ascending, GetKindName, CultureInfo.InvariantCulture, new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc, DayOfWeek.Monday)!;
        var earlierThisMonth = (CatalogDateGroup)grouping.Groups.Single(group => group.Key == nameof(ExplorerGroupBand.DateEarlierThisMonth));

        // Assert
        Assert.Equal(earlierThisMonth.From, earlierThisMonth.Before);
    }

    [Fact]
    public void Should_reverse_the_groups_for_a_descending_direction()
    {
        // Act
        var groups = Group(ExplorerGroupField.Name, CatalogSortDirection.Descending, CreateFile("apple.png"), CreateFile("zebra.png"));

        // Assert
        Assert.Equal([ExplorerGroupBand.NameQToZ, ExplorerGroupBand.NameAToH], groups.Select(group => group.Band));
    }
}
