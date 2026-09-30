namespace Forma.Framework.Catalog.Queries;

using System.Globalization;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

/// <summary>The shaped view over a held item collection: filtered by kind, searched by name, sorted, and cut to the visible page.</summary>
public sealed class CatalogCollectionView
{
    private readonly Func<ICatalogItem, string> _kindLabel;

    private IReadOnlyList<ICatalogItem> _source = [];
    private CatalogResourceFilter? _filter;
    private string _searchQuery = string.Empty;
    private CatalogSortField _sortField = CatalogSortField.Name;
    private CatalogSortDirection _sortDirection = CatalogSortDirection.Ascending;

    private List<ICatalogItem>? _matchingItems;
    private List<ICatalogItem>? _visibleItems;
    private List<ICatalogItem>? _visibleItemsSource;
    private int _visibleItemsSkip;
    private int _visibleItemsTake;

    /// <summary>Creates a view over no items, ordering by kind through the given label of an item's kind.</summary>
    public CatalogCollectionView(Func<ICatalogItem, string> kindLabel)
    {
        ArgumentNullException.ThrowIfNull(kindLabel);

        _kindLabel = kindLabel;
    }

    /// <summary>The items the view shapes.</summary>
    public IReadOnlyList<ICatalogItem> Source
    {
        get => _source;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!ReferenceEquals(_source, value))
            {
                _source = value;
                _matchingItems = null;
            }
        }
    }

    /// <summary>The kind filter an item must pass to be listed, or null to list everything.</summary>
    public CatalogResourceFilter? Filter
    {
        get => _filter;
        set
        {
            if (!ReferenceEquals(_filter, value))
            {
                _filter = value;
                _matchingItems = null;
            }
        }
    }

    /// <summary>The name fragment an item must contain, compared case-insensitively.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!string.Equals(_searchQuery, value, StringComparison.Ordinal))
            {
                _searchQuery = value;
                _matchingItems = null;
            }
        }
    }

    /// <summary>The field the matching items are ordered by.</summary>
    public CatalogSortField SortField
    {
        get => _sortField;
        set
        {
            if (_sortField != value)
            {
                _sortField = value;
                _matchingItems = null;
            }
        }
    }

    /// <summary>The direction the matching items are ordered in.</summary>
    public CatalogSortDirection SortDirection
    {
        get => _sortDirection;
        set
        {
            if (_sortDirection != value)
            {
                _sortDirection = value;
                _matchingItems = null;
            }
        }
    }

    /// <summary>The requested one-based page; reads clamp it through <see cref="EffectivePageNumber"/>.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>How many items one page holds.</summary>
    public int PageSize { get; set; } = 1;

    /// <summary>Whether the visible slice grows from the first page instead of moving, as infinite scrolling needs.</summary>
    public bool IsAppending { get; set; }

    /// <summary>The items that are not hidden and pass the filter and the search, in sort order.</summary>
    public IReadOnlyList<ICatalogItem> MatchingItems
        => MatchingList;

    /// <summary>How many items are not hidden and pass the filter and the search.</summary>
    public int MatchingCount
        => MatchingList.Count;

    /// <summary>How many pages the matching items fill, never less than one.</summary>
    public int PageCount
        => Math.Max(1, (int)Math.Ceiling(MatchingCount / (double)PageSize));

    /// <summary>The requested page clamped to the pages that exist.</summary>
    public int EffectivePageNumber
        => Math.Clamp(PageNumber, 1, PageCount);

    /// <summary>Whether pages remain beyond the visible slice.</summary>
    public bool HasMorePages
        => IsAppending ? VisibleItems.Count < MatchingCount : EffectivePageNumber < PageCount;

    /// <summary>The matching items cut to the visible slice.</summary>
    public IReadOnlyList<ICatalogItem> VisibleItems
    {
        get
        {
            var source = MatchingList;
            var take = IsAppending ? EffectivePageNumber * PageSize : PageSize;
            var skip = IsAppending ? 0 : (EffectivePageNumber - 1) * PageSize;

            // Appending grows this slice to thousands, and rendering reads it several times, so it is rebuilt only when it moves.
            if (_visibleItems is not null && ReferenceEquals(_visibleItemsSource, source) && _visibleItemsSkip == skip && _visibleItemsTake == take)
            {
                return _visibleItems;
            }

            _visibleItemsSource = source;
            _visibleItemsSkip = skip;
            _visibleItemsTake = take;
            _visibleItems = [.. source.Skip(skip).Take(take)];

            return _visibleItems;
        }
    }

    /// <summary>Drops the memoized shape, for a source list mutated in place.</summary>
    public void Invalidate()
        => _matchingItems = null;

    private List<ICatalogItem> MatchingList
    {
        get
        {
            if (_matchingItems is not null)
            {
                return _matchingItems;
            }

            IEnumerable<ICatalogItem> query = _source.Where(item => item.Visibility != CatalogItemVisibility.Hidden);

            if (_filter is { } accepted)
            {
                query = query.Where(item => item.Resource is not { } resource || accepted.Accepts(resource));
            }

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                query = query.Where(item => item.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            query = (_sortField, _sortDirection) switch
            {
                (CatalogSortField.Name, CatalogSortDirection.Ascending) => query.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
                (CatalogSortField.Name, CatalogSortDirection.Descending) => query.OrderByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase),
                (CatalogSortField.Size, CatalogSortDirection.Ascending) => query.OrderBy(item => item.Resource?.Size ?? 0),
                (CatalogSortField.Size, CatalogSortDirection.Descending) => query.OrderByDescending(item => item.Resource?.Size ?? 0),
                (CatalogSortField.Kind, CatalogSortDirection.Ascending) => query.OrderBy(_kindLabel, StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: false)),
                (CatalogSortField.Kind, CatalogSortDirection.Descending) => query.OrderByDescending(_kindLabel, StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: false)),
                (CatalogSortField.DateAdded, CatalogSortDirection.Ascending) => query.OrderBy(item => item.CreatedAt ?? DateTimeOffset.MinValue),
                (CatalogSortField.DateAdded, CatalogSortDirection.Descending) => query.OrderByDescending(item => item.CreatedAt ?? DateTimeOffset.MinValue),
                (CatalogSortField.DateModified, CatalogSortDirection.Ascending) => query.OrderBy(item => item.ModifiedAt ?? DateTimeOffset.MinValue),
                (CatalogSortField.DateModified, CatalogSortDirection.Descending) => query.OrderByDescending(item => item.ModifiedAt ?? DateTimeOffset.MinValue),
                _ => query
            };

            _matchingItems = [.. query];

            return _matchingItems;
        }
    }
}
