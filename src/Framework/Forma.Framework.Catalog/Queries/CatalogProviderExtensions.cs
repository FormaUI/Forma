namespace Forma.Framework.Catalog.Queries;

/// <summary>Reads from a catalog provider.</summary>
public static class CatalogProviderExtensions
{
    /// <summary>Reads every item the query asks for from its <see cref="CatalogQuery.Skip"/> on, a slice of <see cref="CatalogQuery.Take"/> positions at a time.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="query">The query, whose skip is where the reading starts and whose take is the size of each slice.</param>
    /// <param name="cancellationToken">The token cancelling the reading.</param>
    /// <returns>The items, in the order the slices answered them.</returns>
    /// <remarks>
    /// Each slice starts where the last one's positions end, so a slice that holds no item still moves the reading on by the positions
    /// it read. The reading ends at a slice that says no positions follow it; where a slice doesn't say, at one whose positions reach
    /// its total, and with no total either, at one that read fewer positions than it was asked for. A slice that read nothing ends it
    /// too, so a provider that makes no progress is not asked again.
    /// </remarks>
    public static async Task<IReadOnlyList<ICatalogItem>> ReadAllAsync(
        this ICatalogProvider provider,
        CatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentOutOfRangeException.ThrowIfNegative(query.Skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Take);

        List<ICatalogItem> items = [];
        var position = query.Skip;

        while (true)
        {
            var slice = query with { Skip = position };
            var page = await provider.GetItemsAsync(slice, cancellationToken).ConfigureAwait(false);

            items.AddRange(page.Items);
            position += page.ReadCount ?? page.Items.Count;

            if (!page.IsFollowed(slice, position))
            {
                return items;
            }
        }
    }
}
