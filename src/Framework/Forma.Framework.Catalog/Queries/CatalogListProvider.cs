namespace Forma.Framework.Catalog.Queries;

using System.Globalization;

/// <summary>A provider answering from a list of items it holds, searched, filtered, sorted, grouped and sliced as a <see cref="CatalogCollectionView"/> shapes them.</summary>
/// <remarks>It leaves hidden items, those of kinds the query does not ask for and those the query's filter rejects out before slicing, so its positions are its listed items and its total is the count of what is listed.</remarks>
public sealed class CatalogListProvider : ICatalogProvider
{
    private readonly CatalogCollectionView _view;

    /// <summary>Creates the provider over <paramref name="items"/>.</summary>
    /// <param name="items">The items the provider answers from.</param>
    /// <param name="kindLabel">The label of an item's kind, which orders items by kind.</param>
    /// <param name="uiCulture">The culture the kind labels are compared in, read each time the items are ordered.</param>
    public CatalogListProvider(IReadOnlyList<ICatalogItem> items, Func<ICatalogItem, string> kindLabel, Func<CultureInfo> uiCulture)
    {
        ArgumentNullException.ThrowIfNull(items);

        _view = new CatalogCollectionView(kindLabel, uiCulture) { Source = items };
    }

    /// <inheritdoc />
    public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        _view.Filter = query.Filter;
        _view.Kinds = query.Kinds;
        _view.SearchQuery = query.Search ?? string.Empty;
        _view.SortField = query.SortBy;
        _view.SortDirection = query.Direction;
        _view.Grouping = query.Grouping;

        var matching = _view.MatchingItems;

        return ValueTask.FromResult(new CatalogPage([.. matching.Skip(query.Skip).Take(query.Take)], matching.Count)
        {
            ListedCount = matching.Count,
            GroupSizes = query.IncludesGroupSizes ? _view.GroupSizes : null
        });
    }
}
