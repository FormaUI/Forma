namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The shaped view over a held resource collection: filtered by kind, searched by name, sorted, and cut to the visible page.</summary>
public sealed class ExplorerCollectionView
{
    private readonly IResourceKindLabels _kindLabels;

    private IReadOnlyList<ResourceInfo> _source = [];
    private ExplorerResourceFilter? _filter;
    private string _searchQuery = string.Empty;
    private ExplorerSortField _sortField = ExplorerSortField.Name;
    private ExplorerSortDirection _sortDirection = ExplorerSortDirection.Ascending;

    private List<ResourceInfo>? _matchingResources;
    private List<ResourceInfo>? _visibleResources;
    private List<ResourceInfo>? _visibleResourcesSource;
    private int _visibleResourcesSkip;
    private int _visibleResourcesTake;

    public ExplorerCollectionView(IResourceKindLabels kindLabels)
    {
        ArgumentNullException.ThrowIfNull(kindLabels);

        _kindLabels = kindLabels;
    }

    /// <summary>The resources the view shapes.</summary>
    public IReadOnlyList<ResourceInfo> Source
    {
        get => _source;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!ReferenceEquals(_source, value))
            {
                _source = value;
                _matchingResources = null;
            }
        }
    }

    /// <summary>The kind filter a resource must pass to be listed, or null to list everything.</summary>
    public ExplorerResourceFilter? Filter
    {
        get => _filter;
        set
        {
            if (!ReferenceEquals(_filter, value))
            {
                _filter = value;
                _matchingResources = null;
            }
        }
    }

    /// <summary>The name fragment a resource must contain, compared case-insensitively.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!string.Equals(_searchQuery, value, StringComparison.Ordinal))
            {
                _searchQuery = value;
                _matchingResources = null;
            }
        }
    }

    /// <summary>The field the matching resources are ordered by.</summary>
    public ExplorerSortField SortField
    {
        get => _sortField;
        set
        {
            if (_sortField != value)
            {
                _sortField = value;
                _matchingResources = null;
            }
        }
    }

    /// <summary>The direction the matching resources are ordered in.</summary>
    public ExplorerSortDirection SortDirection
    {
        get => _sortDirection;
        set
        {
            if (_sortDirection != value)
            {
                _sortDirection = value;
                _matchingResources = null;
            }
        }
    }

    /// <summary>The requested one-based page; reads clamp it through <see cref="EffectivePageNumber"/>.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>How many resources one page holds.</summary>
    public int PageSize { get; set; } = 1;

    /// <summary>Whether the visible window grows from the first page instead of moving, as infinite scrolling needs.</summary>
    public bool IsAppending { get; set; }

    /// <summary>The resources that pass the filter and the search, in sort order.</summary>
    public IReadOnlyList<ResourceInfo> MatchingResources
        => MatchingList;

    /// <summary>How many resources pass the filter and the search.</summary>
    public int MatchingCount
        => MatchingList.Count;

    /// <summary>How many pages the matching resources fill, never less than one.</summary>
    public int PageCount
        => Math.Max(1, (int)Math.Ceiling(MatchingCount / (double)PageSize));

    /// <summary>The requested page clamped to the pages that exist.</summary>
    public int EffectivePageNumber
        => Math.Clamp(PageNumber, 1, PageCount);

    /// <summary>Whether pages remain beyond the visible window.</summary>
    public bool HasMorePages
        => IsAppending ? VisibleResources.Count < MatchingCount : EffectivePageNumber < PageCount;

    /// <summary>The matching resources cut to the visible window.</summary>
    public IReadOnlyList<ResourceInfo> VisibleResources
    {
        get
        {
            var source = MatchingList;
            var take = IsAppending ? EffectivePageNumber * PageSize : PageSize;
            var skip = IsAppending ? 0 : (EffectivePageNumber - 1) * PageSize;

            // Appending grows this window to thousands, and rendering reads it several times, so it is rebuilt only when it moves.
            if (_visibleResources is not null && ReferenceEquals(_visibleResourcesSource, source) && _visibleResourcesSkip == skip && _visibleResourcesTake == take)
            {
                return _visibleResources;
            }

            _visibleResourcesSource = source;
            _visibleResourcesSkip = skip;
            _visibleResourcesTake = take;
            _visibleResources = [.. source.Skip(skip).Take(take)];

            return _visibleResources;
        }
    }

    /// <summary>Drops the memoized shape, for a source list mutated in place.</summary>
    public void Invalidate()
        => _matchingResources = null;

    private List<ResourceInfo> MatchingList
    {
        get
        {
            if (_matchingResources is not null)
            {
                return _matchingResources;
            }

            IEnumerable<ResourceInfo> query = _source;

            if (_filter is { } accepted)
            {
                query = query.Where(accepted.Accepts);
            }

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                query = query.Where(resource => resource.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            query = (_sortField, _sortDirection) switch
            {
                (ExplorerSortField.Name, ExplorerSortDirection.Ascending) => query.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase),
                (ExplorerSortField.Name, ExplorerSortDirection.Descending) => query.OrderByDescending(resource => resource.Name, StringComparer.OrdinalIgnoreCase),
                (ExplorerSortField.Size, ExplorerSortDirection.Ascending) => query.OrderBy(resource => resource.Size ?? 0),
                (ExplorerSortField.Size, ExplorerSortDirection.Descending) => query.OrderByDescending(resource => resource.Size ?? 0),
                (ExplorerSortField.Kind, ExplorerSortDirection.Ascending) => query.OrderBy(_kindLabels.LabelOf, StringComparer.CurrentCulture),
                (ExplorerSortField.Kind, ExplorerSortDirection.Descending) => query.OrderByDescending(_kindLabels.LabelOf, StringComparer.CurrentCulture),
                (ExplorerSortField.DateAdded, ExplorerSortDirection.Ascending) => query.OrderBy(resource => resource.CreatedAt ?? DateTimeOffset.MinValue),
                (ExplorerSortField.DateAdded, ExplorerSortDirection.Descending) => query.OrderByDescending(resource => resource.CreatedAt ?? DateTimeOffset.MinValue),
                (ExplorerSortField.DateModified, ExplorerSortDirection.Ascending) => query.OrderBy(resource => resource.ModifiedAt ?? DateTimeOffset.MinValue),
                (ExplorerSortField.DateModified, ExplorerSortDirection.Descending) => query.OrderByDescending(resource => resource.ModifiedAt ?? DateTimeOffset.MinValue),
                _ => query
            };

            _matchingResources = [.. query];

            return _matchingResources;
        }
    }
}
