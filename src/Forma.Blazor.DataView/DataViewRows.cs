namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>The rows a virtualizing view draws over its data view: a heading for each group where the data view groups, and the items in rows of as many as stand across.</summary>
/// <typeparam name="TItem">The item the rows hold.</typeparam>
/// <remarks>
/// The rows are laid out as segments, one for an ungrouped set and one per group otherwise, each a heading row where it has a group
/// followed by its item rows unless the group is folded. A slice of rows is answered by fetching the item ranges those rows cover,
/// one fetch per run of consecutive items, so a folded group between two open ones is never read. Over a source that cannot
/// count, the rows run one slice past the last item answered until a slice comes back shorter than asked for, which ends the
/// set, so a scroll to their end asks for the next slice and no request reaches further than one. The rows found by a fetch are
/// published after it, never on the answer that found them, since the framework's virtualization holds the viewport where it was
/// when the rows grow under a slice that reaches their end; a scroll the view asks for holds them back until it has landed.
/// </remarks>
/// <param name="view">The data view the rows are laid out over.</param>
/// <param name="virtualizer">The virtualization the view draws the rows through, once it is drawn.</param>
/// <param name="invoke">Runs work on the view's renderer.</param>
/// <param name="redraw">Draws the view again.</param>
internal sealed class DataViewRows<TItem>(IDataView<TItem> view, Func<Virtualize<DataViewRow<TItem>>?> virtualizer, Func<Func<Task>, Task> invoke, Action redraw)
    where TItem : notnull
{
    // Room enough for any set a source cannot count, which is read until it comes back short.
    private const int UncountedItems = int.MaxValue / 2;

    private readonly IDataView<TItem> _view = view;
    private readonly RowComparer _comparer = new(view.ItemComparer);
    private List<Segment> _segments = [];
    private List<(object? Key, int Count, bool IsFolded)> _shape = [];
    private int? _shapeTotal;
    private int _shapeColumns;
    private bool _isLaidOut;
    private int _columns = 1;
    private int _knownItems;
    private bool _isExhausted;
    private int _publishedRows;
    private int _continuationRows;
    private bool _isHeld;

    /// <summary>How many items stand across a row.</summary>
    public int Columns
    {
        get => _columns;
        set => _columns = Math.Max(1, value);
    }

    /// <summary>How many rows the set lays out into.</summary>
    public int RowCount { get; private set; }

    /// <summary>Tells a row from a re-fetched copy of it: the same group's heading, or a row opening on the same item.</summary>
    public IEqualityComparer<DataViewRow<TItem>> Comparer
        => _comparer;

    /// <summary>Forgets what the rows were laid out from and what a source that cannot count has answered, for a data view whose source, sort or set changed.</summary>
    public void Reset()
    {
        _isLaidOut = false;
        _knownItems = 0;
        _isExhausted = false;
        _publishedRows = 0;
        _continuationRows = 0;
    }

    /// <summary>Lays the rows out again from the data view's count, groups and folds and the columns across; true when the rows changed, so the view fetches its slices again.</summary>
    public bool Update()
    {
        List<(object? Key, int Count, bool IsFolded)> shape = [.. _view.Groups.Select(group => (group.Key, group.Count, _view.IsFolded(group)))];

        if (_isLaidOut && _shapeTotal == _view.TotalItemCount && _shapeColumns == _columns && shape.SequenceEqual(_shape))
        {
            return false;
        }

        _isLaidOut = true;
        _shape = shape;
        _shapeTotal = _view.TotalItemCount;
        _shapeColumns = _columns;
        _segments = [];
        var row = 0;

        if (_view.IsGrouped)
        {
            var firstItem = 0;

            foreach (var group in _view.Groups)
            {
                var segment = new Segment(row, group, firstItem, group.Count, _view.IsFolded(group));
                _segments.Add(segment);
                row += segment.GetRowCount(_columns);
                firstItem += group.Count;
            }
        }
        else if (_view.TotalItemCount is { } total and > 0)
        {
            var segment = new Segment(0, null, 0, total, false);
            _segments.Add(segment);
            row = segment.GetRowCount(_columns);
        }
        else if (_view.TotalItemCount is null)
        {
            // A source that cannot count is read as far as it answers; the rows end where a slice comes back short.
            var segment = new Segment(0, null, 0, UncountedItems, false);
            _segments.Add(segment);
            row = segment.GetRowCount(_columns);
        }

        RowCount = row;

        return true;
    }

    /// <summary>The row that holds the item at <paramref name="itemIndex"/> in the whole set, or its group's heading where the item stands in the group's first row or the group is folded.</summary>
    /// <param name="itemIndex">The index of the item in the whole set.</param>
    public int GetRowIndex(int itemIndex)
    {
        foreach (var segment in _segments)
        {
            if (itemIndex < segment.FirstItem + segment.ItemCount)
            {
                var itemRow = Math.Max(0, itemIndex - segment.FirstItem) / _columns;

                // A group's first row is reached with its heading above it, so the reader sees which group it opens.
                return segment.IsFolded || itemRow == 0
                    ? segment.FirstRow
                    : segment.FirstRow + (segment.Group is null ? 0 : 1) + itemRow;
            }
        }

        return Math.Max(0, RowCount - 1);
    }

    /// <summary>Answers the framework's virtualization with the rows it asks for, fetching the items those rows hold.</summary>
    /// <param name="request">The rows asked for.</param>
    public async ValueTask<ItemsProviderResult<DataViewRow<TItem>>> ProvideAsync(ItemsProviderRequest request)
    {
        var end = Math.Min(RowCount, request.StartIndex + request.Count);
        List<(DataViewGroup<TItem>? Group, int FirstItem, int ItemCount)> rows = [];

        for (var row = request.StartIndex; row < end; row++)
        {
            var segment = FindSegment(row);
            var offset = row - segment.FirstRow;

            if (segment.Group is { } group && offset == 0)
            {
                rows.Add((group, segment.FirstItem, 0));
                continue;
            }

            var itemRow = offset - (segment.Group is null ? 0 : 1);
            var firstItem = segment.FirstItem + (itemRow * _columns);
            rows.Add((null, firstItem, Math.Min(_columns, segment.FirstItem + segment.ItemCount - firstItem)));
        }

        var items = await FetchAsync(rows, request.CancellationToken);
        List<DataViewRow<TItem>> answered = [.. rows.Select(row => new DataViewRow<TItem>(row.Group, row.Group is null ? Slice(items, row.FirstItem, row.ItemCount) : [], row.FirstItem))];

        if (_view.TotalItemCount is not null || _view.IsGrouped)
        {
            return new ItemsProviderResult<DataViewRow<TItem>>(answered, RowCount);
        }

        // Uncounted, a slice shorter than asked for ends the set; until one does, the rows run one slice past the last answered,
        // so a scroll to their end asks for the next.
        var filled = answered.TakeWhile(row => row.Items.Count > 0).ToList();

        if (filled.Count > 0)
        {
            _knownItems = Math.Max(_knownItems, filled[^1].FirstItemIndex + filled[^1].Items.Count);
        }

        _isExhausted |= filled.Count < rows.Count || (filled.Count > 0 && filled[^1].Items.Count < _columns);
        _continuationRows = Math.Max(_continuationRows, request.Count);

        // The first answer is published with it; a later one that found more rows is published once it has been drawn.
        if (_publishedRows == 0)
        {
            Publish();
        }
        else if (!_isHeld && PublishableRows != _publishedRows)
        {
            _ = invoke(RepublishAsync);
        }

        return new ItemsProviderResult<DataViewRow<TItem>>(filled, _publishedRows);
    }

    /// <summary>Scrolls the virtualization to the row holding the item at <paramref name="itemIndex"/>, publishing the rows found so far first and holding back the ones the scroll finds until it has landed.</summary>
    /// <param name="itemIndex">The index of the item in the whole set.</param>
    /// <param name="cancellationToken">Canceled when the scroll is no longer wanted.</param>
    public async ValueTask ScrollToAsync(int itemIndex, CancellationToken cancellationToken)
    {
        if (virtualizer() is not { } rows)
        {
            return;
        }

        _isHeld = true;

        try
        {
            if (Publish())
            {
                await rows.RefreshDataAsync();
                redraw();
            }

            await rows.ScrollToItemAsync(GetRowIndex(itemIndex), cancellationToken);
        }
        finally
        {
            _isHeld = false;
        }

        await RepublishAsync();
    }

    // Uncounted, the rows only ever grow: every item answered so far, and one slice more while the source may hold further ones.
    private int PublishableRows
        => ((_knownItems + _columns - 1) / _columns) + (_isExhausted ? 0 : _continuationRows);

    private bool IsUncounted
        => _view.TotalItemCount is null && !_view.IsGrouped;

    private bool Publish()
    {
        if (!IsUncounted || PublishableRows == _publishedRows)
        {
            return false;
        }

        _publishedRows = PublishableRows;

        return true;
    }

    // Deferred past the answer that found the rows, which the virtualization is still taking in.
    private async Task RepublishAsync()
    {
        await Task.Yield();

        if (!_isHeld && Publish() && virtualizer() is { } rows)
        {
            await rows.RefreshDataAsync();
            redraw();
        }
    }

    /// <summary>The position in the set of the first or the last item the rows reach, across the open groups, or null where they reach none; uncounted, the last the source has answered.</summary>
    /// <param name="last">Whether the last is asked for rather than the first.</param>
    public int? GetEdgePosition(bool last)
    {
        if (_view.TotalItemCount is null && !_view.IsGrouped)
        {
            return _knownItems == 0 ? null : last ? _knownItems - 1 : 0;
        }

        var open = _segments.Where(segment => !segment.IsFolded && segment.ItemCount > 0).ToList();

        return open.Count == 0 ? null
            : last ? open[^1].FirstItem + open[^1].ItemCount - 1
            : open[0].FirstItem;
    }

    /// <summary>The items the rows reach from <paramref name="from"/> to <paramref name="to"/> in the set, both included, a folded group's items left out.</summary>
    /// <param name="from">The position of the first item.</param>
    /// <param name="to">The position of the last item.</param>
    /// <param name="cancellationToken">Canceled when the items are no longer wanted.</param>
    public async ValueTask<IReadOnlyList<TItem>> ReadAsync(int from, int to, CancellationToken cancellationToken)
    {
        List<TItem> items = [];

        foreach (var segment in _segments.Where(segment => !segment.IsFolded))
        {
            var start = Math.Max(from, segment.FirstItem);
            var stop = Math.Min(to + 1, segment.FirstItem + segment.ItemCount);

            if (stop > start && !cancellationToken.IsCancellationRequested)
            {
                var page = await _view.LoadSliceAsync(start, stop - start, cancellationToken);
                items.AddRange(page.Items);
            }
        }

        return items;
    }

    // The items the rows hold, read in one slice per run of consecutive items and kept by their index in the set.
    private async ValueTask<Dictionary<int, TItem>> FetchAsync(List<(DataViewGroup<TItem>? Group, int FirstItem, int ItemCount)> rows, CancellationToken cancellationToken)
    {
        Dictionary<int, TItem> items = [];
        var runStart = -1;
        var runEnd = -1;

        foreach (var (_, firstItem, itemCount) in rows.Where(row => row.ItemCount > 0))
        {
            if (firstItem == runEnd)
            {
                runEnd += itemCount;
                continue;
            }

            await ReadAsync(runStart, runEnd);
            runStart = firstItem;
            runEnd = firstItem + itemCount;
        }

        await ReadAsync(runStart, runEnd);

        return items;

        async ValueTask ReadAsync(int start, int stop)
        {
            if (start < 0 || stop <= start || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var page = await _view.LoadSliceAsync(start, stop - start, cancellationToken);

            for (var offset = 0; offset < page.Items.Count; offset++)
            {
                items[start + offset] = page.Items[offset];
            }
        }
    }

    private static List<TItem> Slice(Dictionary<int, TItem> items, int firstItem, int itemCount)
    {
        List<TItem> slice = [];

        for (var index = firstItem; index < firstItem + itemCount; index++)
        {
            if (items.TryGetValue(index, out var item))
            {
                slice.Add(item);
            }
        }

        return slice;
    }

    // The segments stand in row order, so the last one opening at or before the row holds it.
    private Segment FindSegment(int row)
    {
        var low = 0;
        var high = _segments.Count - 1;

        while (low < high)
        {
            var middle = (low + high + 1) / 2;

            if (_segments[middle].FirstRow <= row)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        return _segments[low];
    }

    /// <summary>A run of rows: an ungrouped set's items, or one group's heading followed by its items unless it is folded.</summary>
    private readonly record struct Segment(int FirstRow, DataViewGroup<TItem>? Group, int FirstItem, int ItemCount, bool IsFolded)
    {
        public int GetRowCount(int columns)
            => (Group is null ? 0 : 1) + (IsFolded ? 0 : (ItemCount + columns - 1) / columns);
    }

    /// <summary>Tells a heading row by its group's key and a row of items by its first item.</summary>
    private sealed class RowComparer(IEqualityComparer<TItem> items) : IEqualityComparer<DataViewRow<TItem>>
    {
        public bool Equals(DataViewRow<TItem>? x, DataViewRow<TItem>? y)
            => (x, y) switch
            {
                (null, null) => true,
                (null, _) or (_, null) => false,
                ({ Group: { } left }, { Group: { } right }) => Equals(left.Key, right.Key),
                ({ Group: null, Items: [var left, ..] }, { Group: null, Items: [var right, ..] }) => items.Equals(left, right),
                _ => false,
            };

        public int GetHashCode(DataViewRow<TItem> obj)
            => obj switch
            {
                { Group: { } group } => group.Key?.GetHashCode() ?? 0,
                { Items: [var first, ..] } => items.GetHashCode(first),
                _ => 0,
            };
    }
}
