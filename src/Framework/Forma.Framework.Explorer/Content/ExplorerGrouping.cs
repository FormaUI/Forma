namespace Forma.Framework.Explorer.Content;

using System.Globalization;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;

/// <summary>Gathers a list of items into the groups a field bands them by.</summary>
public static class ExplorerGrouping
{
    private const long Kilobyte = 1024;

    private const long Megabyte = Kilobyte * 1024;

    private const long Gigabyte = Megabyte * 1024;

    /// <summary>Gathers the items into groups by the field.</summary>
    /// <remarks>Each group keeps its items in list order; the groups stand in band order, kinds ordered by label, and the whole order reverses for a descending direction.</remarks>
    /// <param name="items">The items to group.</param>
    /// <param name="field">The field the items are grouped by.</param>
    /// <param name="direction">The direction the groups are ordered in.</param>
    /// <param name="kindLabel">The label of an item's kind.</param>
    /// <param name="uiCulture">The culture the kind labels are compared in.</param>
    /// <param name="now">The moment dates are banded against.</param>
    /// <param name="firstDayOfWeek">The day a week starts on.</param>
    /// <returns>The groups of the items.</returns>
    public static IReadOnlyList<ExplorerItemGroup> GroupBy(
        IReadOnlyList<ICatalogItem> items,
        ExplorerGroupField field,
        CatalogSortDirection direction,
        Func<ICatalogItem, string> kindLabel,
        CultureInfo uiCulture,
        DateTimeOffset now,
        DayOfWeek firstDayOfWeek)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(kindLabel);
        ArgumentNullException.ThrowIfNull(uiCulture);

        if (field == ExplorerGroupField.None)
        {
            return [];
        }

        var groups = new Dictionary<string, (ExplorerGroupBand Band, string? KindLabel, List<ICatalogItem> Items)>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            var (band, label) = GetBand(item, field, kindLabel, now, firstDayOfWeek);
            var key = ExplorerItemGroup.GetKey(band, label);

            if (!groups.TryGetValue(key, out var group))
            {
                group = (band, label, []);
                groups[key] = group;
            }

            group.Items.Add(item);
        }

        var comparer = StringComparer.Create(uiCulture, ignoreCase: false);
        var ordered = groups.Values
            .OrderBy(group => group.Band)
            .ThenBy(group => group.KindLabel, comparer);

        var result = ordered.Select(group => new ExplorerItemGroup(group.Band, group.KindLabel, group.Items));

        return direction == CatalogSortDirection.Descending ? [.. result.Reverse()] : [.. result];
    }

    private static (ExplorerGroupBand Band, string? KindLabel) GetBand(ICatalogItem item, ExplorerGroupField field, Func<ICatalogItem, string> kindLabel, DateTimeOffset now, DayOfWeek firstDayOfWeek)
        => field switch
        {
            ExplorerGroupField.Name => (GetNameBand(item.Name), null),
            ExplorerGroupField.Size => (GetSizeBand(item.Resource?.Size), null),
            ExplorerGroupField.Kind => (ExplorerGroupBand.Kind, kindLabel(item)),
            ExplorerGroupField.DateAdded => (GetDateBand(item.CreatedAt, now, firstDayOfWeek), null),
            ExplorerGroupField.DateModified => (GetDateBand(item.ModifiedAt, now, firstDayOfWeek), null),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
        };

    private static ExplorerGroupBand GetNameBand(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return ExplorerGroupBand.NameOther;
        }

        var first = char.ToUpperInvariant(name[0]);

        return first switch
        {
            >= '0' and <= '9' => ExplorerGroupBand.NameDigits,
            >= 'A' and <= 'H' => ExplorerGroupBand.NameAToH,
            >= 'I' and <= 'P' => ExplorerGroupBand.NameIToP,
            >= 'Q' and <= 'Z' => ExplorerGroupBand.NameQToZ,
            _ => ExplorerGroupBand.NameOther,
        };
    }

    private static ExplorerGroupBand GetSizeBand(long? size)
        => size switch
        {
            null => ExplorerGroupBand.SizeUnspecified,
            <= 0 => ExplorerGroupBand.SizeEmpty,
            <= 16 * Kilobyte => ExplorerGroupBand.SizeTiny,
            <= Megabyte => ExplorerGroupBand.SizeSmall,
            <= 128 * Megabyte => ExplorerGroupBand.SizeMedium,
            <= Gigabyte => ExplorerGroupBand.SizeLarge,
            <= 4 * Gigabyte => ExplorerGroupBand.SizeHuge,
            _ => ExplorerGroupBand.SizeGigantic,
        };

    private static ExplorerGroupBand GetDateBand(DateTimeOffset? moment, DateTimeOffset now, DayOfWeek firstDayOfWeek)
    {
        if (moment is not { } value)
        {
            return ExplorerGroupBand.DateUnspecified;
        }

        var today = now.ToLocalTime().Date;
        var day = value.ToLocalTime().Date;

        if (day >= today)
        {
            return ExplorerGroupBand.DateToday;
        }

        if (day == today.AddDays(-1))
        {
            return ExplorerGroupBand.DateYesterday;
        }

        var weekStart = today.AddDays(-(((int)today.DayOfWeek - (int)firstDayOfWeek + 7) % 7));

        if (day >= weekStart)
        {
            return ExplorerGroupBand.DateEarlierThisWeek;
        }

        if (day >= weekStart.AddDays(-7))
        {
            return ExplorerGroupBand.DateLastWeek;
        }

        var monthStart = new DateTime(today.Year, today.Month, 1);

        if (day >= monthStart)
        {
            return ExplorerGroupBand.DateEarlierThisMonth;
        }

        if (day >= monthStart.AddMonths(-1))
        {
            return ExplorerGroupBand.DateLastMonth;
        }

        return day.Year == today.Year ? ExplorerGroupBand.DateEarlierThisYear : ExplorerGroupBand.DateLongAgo;
    }
}
