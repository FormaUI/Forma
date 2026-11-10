namespace Forma.Framework.Explorer.Content;

using System.Globalization;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;

/// <summary>Defines the groups a field bands catalog items into, and gathers a list of items into them.</summary>
public static class ExplorerGrouping
{
    private const long Kilobyte = 1024;

    private const long Megabyte = Kilobyte * 1024;

    private const long Gigabyte = Megabyte * 1024;

    // The prefix of a kind group's key, ahead of the kind's name.
    private const string KindKeyPrefix = "Kind.";

    /// <summary>The groups <paramref name="field"/> bands catalog items into, defined against <paramref name="now"/>, where <see cref="ExplorerGroupField.None"/> has none.</summary>
    /// <remarks>
    /// The groups stand in band order, kinds in the order of their labels in <paramref name="uiCulture"/>, and the whole
    /// order reverses for a descending direction. A date band holds the days of <paramref name="timeZone"/> it names, from
    /// the instant its first day begins to the instant the band after it in time begins, so the definition stays the same
    /// for every slice read against it.
    /// </remarks>
    /// <param name="field">The field the items are grouped by.</param>
    /// <param name="direction">The direction the groups are ordered in.</param>
    /// <param name="kindLabel">The label of a kind.</param>
    /// <param name="uiCulture">The culture the kind labels are compared in.</param>
    /// <param name="now">The moment dates are banded against.</param>
    /// <param name="timeZone">The time zone whose days the dates are banded by.</param>
    /// <param name="firstDayOfWeek">The day a week starts on.</param>
    public static CatalogGrouping? Define(
        ExplorerGroupField field,
        CatalogSortDirection direction,
        Func<CatalogItemKind, string> kindLabel,
        CultureInfo uiCulture,
        DateTimeOffset now,
        TimeZoneInfo timeZone,
        DayOfWeek firstDayOfWeek)
    {
        ArgumentNullException.ThrowIfNull(kindLabel);
        ArgumentNullException.ThrowIfNull(uiCulture);
        ArgumentNullException.ThrowIfNull(timeZone);

        (CatalogGroupField Field, List<CatalogGroup> Groups)? definition = field switch
        {
            ExplorerGroupField.None => null,
            ExplorerGroupField.Name => (CatalogGroupField.Name, DefineNameGroups()),
            ExplorerGroupField.Size => (CatalogGroupField.Size, DefineSizeGroups()),
            ExplorerGroupField.Kind => (CatalogGroupField.Kind, DefineKindGroups(kindLabel, uiCulture)),
            ExplorerGroupField.DateAdded => (CatalogGroupField.DateAdded, DefineDateGroups(now, timeZone, firstDayOfWeek)),
            ExplorerGroupField.DateModified => (CatalogGroupField.DateModified, DefineDateGroups(now, timeZone, firstDayOfWeek)),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
        };

        if (definition is null)
        {
            return null;
        }

        var (catalogField, groups) = definition.Value;

        if (direction == CatalogSortDirection.Descending)
        {
            groups.Reverse();
        }

        return new CatalogGrouping(catalogField, groups);
    }

    /// <summary>Gathers the items into the groups of <paramref name="grouping"/> that hold any, in the order the groups stand.</summary>
    /// <param name="items">The items to group, each group keeping them in list order.</param>
    /// <param name="grouping">The groups the items gather into, as <see cref="Define"/> built them.</param>
    /// <param name="kindLabel">The label of a kind.</param>
    /// <param name="sizes">How many items each group holds across the whole result, where a group the sizes leave out counts its own items.</param>
    public static IReadOnlyList<ExplorerItemGroup> GroupBy(
        IReadOnlyList<ICatalogItem> items,
        CatalogGrouping? grouping,
        Func<CatalogItemKind, string> kindLabel,
        IReadOnlyList<CatalogGroupSize>? sizes = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(kindLabel);

        if (grouping is null)
        {
            return [];
        }

        var members = new List<ICatalogItem>?[grouping.Groups.Count];

        foreach (var item in items)
        {
            (members[grouping.IndexOf(item)] ??= []).Add(item);
        }

        var counts = sizes?.ToDictionary(size => size.Key, size => size.Count, StringComparer.Ordinal);
        List<ExplorerItemGroup> result = [];

        for (var index = 0; index < members.Length; index++)
        {
            if (members[index] is not { } groupItems)
            {
                continue;
            }

            var group = grouping.Groups[index];
            var (band, label) = group is CatalogKindGroup kindGroup
                ? (ExplorerGroupBand.Kind, kindLabel(kindGroup.Kind))
                : (Enum.Parse<ExplorerGroupBand>(group.Key), null);
            var count = counts?.GetValueOrDefault(group.Key, groupItems.Count) ?? groupItems.Count;

            result.Add(new ExplorerItemGroup(group.Key, band, label, groupItems, count));
        }

        return result;
    }

    private static List<CatalogGroup> DefineNameGroups()
        =>
        [
            new CatalogNameGroup(nameof(ExplorerGroupBand.NameDigits), '0', '9'),
            new CatalogNameGroup(nameof(ExplorerGroupBand.NameAToH), 'A', 'H'),
            new CatalogNameGroup(nameof(ExplorerGroupBand.NameIToP), 'I', 'P'),
            new CatalogNameGroup(nameof(ExplorerGroupBand.NameQToZ), 'Q', 'Z'),
            new CatalogRemainderGroup(nameof(ExplorerGroupBand.NameOther)),
        ];

    private static List<CatalogGroup> DefineSizeGroups()
        =>
        [
            new CatalogMissingGroup(nameof(ExplorerGroupBand.SizeUnspecified)),
            new CatalogSizeGroup(nameof(ExplorerGroupBand.SizeEmpty), null, 0),
            new CatalogSizeGroup(nameof(ExplorerGroupBand.SizeTiny), 0, 16 * Kilobyte),
            new CatalogSizeGroup(nameof(ExplorerGroupBand.SizeSmall), 16 * Kilobyte, Megabyte),
            new CatalogSizeGroup(nameof(ExplorerGroupBand.SizeMedium), Megabyte, 128 * Megabyte),
            new CatalogSizeGroup(nameof(ExplorerGroupBand.SizeLarge), 128 * Megabyte, Gigabyte),
            new CatalogSizeGroup(nameof(ExplorerGroupBand.SizeHuge), Gigabyte, 4 * Gigabyte),
            new CatalogSizeGroup(nameof(ExplorerGroupBand.SizeGigantic), 4 * Gigabyte, null),
        ];

    private static List<CatalogGroup> DefineKindGroups(Func<CatalogItemKind, string> kindLabel, CultureInfo uiCulture)
    {
        var comparer = StringComparer.Create(uiCulture, ignoreCase: false);

        return
        [
            .. Enum.GetValues<CatalogItemKind>()
                .OrderBy(kindLabel, comparer)
                .Select(kind => new CatalogKindGroup(KindKeyPrefix + kind, kind))
        ];
    }

    private static List<CatalogGroup> DefineDateGroups(DateTimeOffset now, TimeZoneInfo timeZone, DayOfWeek firstDayOfWeek)
    {
        var today = TimeZoneInfo.ConvertTime(now, timeZone).Date;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek - (int)firstDayOfWeek + 7) % 7));
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

        // Each band's first day, newest first; a band ends where the newer bands begin.
        (ExplorerGroupBand Band, DateTime? FirstDay)[] bands =
        [
            (ExplorerGroupBand.DateToday, today),
            (ExplorerGroupBand.DateYesterday, today.AddDays(-1)),
            (ExplorerGroupBand.DateEarlierThisWeek, weekStart),
            (ExplorerGroupBand.DateLastWeek, weekStart.AddDays(-7)),
            (ExplorerGroupBand.DateEarlierThisMonth, monthStart),
            (ExplorerGroupBand.DateLastMonth, monthStart.AddMonths(-1)),
            (ExplorerGroupBand.DateEarlierThisYear, new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
            (ExplorerGroupBand.DateLongAgo, null),
        ];

        List<CatalogGroup> groups = [new CatalogMissingGroup(nameof(ExplorerGroupBand.DateUnspecified))];
        DateTimeOffset? newerStart = null;

        foreach (var (band, firstDay) in bands)
        {
            DateTimeOffset? start = firstDay is { } day ? new DateTimeOffset(day, timeZone.GetUtcOffset(day)) : null;

            // A band whose days a newer band already holds is empty.
            if (start is { } from && newerStart is { } newer && from > newer)
            {
                start = newer;
            }

            groups.Add(new CatalogDateGroup(band.ToString(), start, newerStart));
            newerStart = start;
        }

        return groups;
    }
}
