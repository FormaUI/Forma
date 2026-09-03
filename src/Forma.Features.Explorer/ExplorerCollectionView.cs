namespace Forma.Features.Explorer;

using System.Globalization;

using Forma.Framework.Resources;

/// <summary>The shaped view over a held item collection: filtered by kind, searched by name, sorted, and cut to the visible page.</summary>
public sealed class ExplorerCollectionView
{
    private readonly Func<IExplorerItem, string> _kindLabel;

    private IReadOnlyList<IExplorerItem> _source = [];
    private ExplorerResourceFilter? _filter;
    private string _searchQuery = string.Empty;
    private ExplorerSortField _sortField = ExplorerSortField.Name;
    private ExplorerSortDirection _sortDirection = ExplorerSortDirection.Ascending;

    private List<IExplorerItem>? _matchingItems;
    private List<IExplorerItem>? _visibleItems;
    private List<IExplorerItem>? _visibleItemsSource;
    private int _visibleItemsSkip;
    private int _visibleItemsTake;

    public ExplorerCollectionView(Func<IExplorerItem, string> kindLabel)
    {
        ArgumentNullException.ThrowIfNull(kindLabel);

        _kindLabel = kindLabel;
    }

    /// <summary>The items the view shapes.</summary>
    public IReadOnlyList<IExplorerItem> Source
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
    public ExplorerResourceFilter? Filter
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
    public ExplorerSortField SortField
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
    public ExplorerSortDirection SortDirection
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

    /// <summary>Whether the visible window grows from the first page instead of moving, as infinite scrolling needs.</summary>
    public bool IsAppending { get; set; }

    /// <summary>The items that are not hidden and pass the filter and the search, in sort order.</summary>
    public IReadOnlyList<IExplorerItem> MatchingItems
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

    /// <summary>Whether pages remain beyond the visible window.</summary>
    public bool HasMorePages
        => IsAppending ? VisibleItems.Count < MatchingCount : EffectivePageNumber < PageCount;

    /// <summary>The matching items cut to the visible window.</summary>
    public IReadOnlyList<IExplorerItem> VisibleItems
    {
        get
        {
            var source = MatchingList;
            var take = IsAppending ? EffectivePageNumber * PageSize : PageSize;
            var skip = IsAppending ? 0 : (EffectivePageNumber - 1) * PageSize;

            // Appending grows this window to thousands, and rendering reads it several times, so it is rebuilt only when it moves.
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

    private List<IExplorerItem> MatchingList
    {
        get
        {
            if (_matchingItems is not null)
            {
                return _matchingItems;
            }

            IEnumerable<IExplorerItem> query = _source.Where(item => item.Visibility != ExplorerItemVisibility.Hidden);

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
                (ExplorerSortField.Name, ExplorerSortDirection.Ascending) => query.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
                (ExplorerSortField.Name, ExplorerSortDirection.Descending) => query.OrderByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase),
                (ExplorerSortField.Size, ExplorerSortDirection.Ascending) => query.OrderBy(item => item.Resource?.Size ?? 0),
                (ExplorerSortField.Size, ExplorerSortDirection.Descending) => query.OrderByDescending(item => item.Resource?.Size ?? 0),
                (ExplorerSortField.Kind, ExplorerSortDirection.Ascending) => query.OrderBy(_kindLabel, StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: false)),
                (ExplorerSortField.Kind, ExplorerSortDirection.Descending) => query.OrderByDescending(_kindLabel, StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: false)),
                (ExplorerSortField.DateAdded, ExplorerSortDirection.Ascending) => query.OrderBy(item => item.CreatedAt ?? DateTimeOffset.MinValue),
                (ExplorerSortField.DateAdded, ExplorerSortDirection.Descending) => query.OrderByDescending(item => item.CreatedAt ?? DateTimeOffset.MinValue),
                (ExplorerSortField.DateModified, ExplorerSortDirection.Ascending) => query.OrderBy(item => item.ModifiedAt ?? DateTimeOffset.MinValue),
                (ExplorerSortField.DateModified, ExplorerSortDirection.Descending) => query.OrderByDescending(item => item.ModifiedAt ?? DateTimeOffset.MinValue),
                _ => query
            };

            _matchingItems = [.. query];

            return _matchingItems;
        }
    }
}
