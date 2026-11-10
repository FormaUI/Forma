namespace Forma.Framework.Catalog.Queries;

/// <summary>The items whose date of the grouped field falls from <paramref name="From"/>, included, to <paramref name="Before"/>, excluded, where a null bound is open.</summary>
/// <param name="Key">The group's key.</param>
/// <param name="From">The earliest instant of the group.</param>
/// <param name="Before">The instant the group ends before.</param>
public sealed record CatalogDateGroup(string Key, DateTimeOffset? From, DateTimeOffset? Before) : CatalogGroup(Key)
{
    /// <inheritdoc />
    public override bool Admits(ICatalogItem item, CatalogGroupField field)
    {
        ArgumentNullException.ThrowIfNull(item);

        return CatalogGrouping.GetDate(item, field) is { } date
            && (From is not { } from || date >= from)
            && (Before is not { } before || date < before);
    }
}
