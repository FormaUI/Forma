namespace Forma.Blazor.Components.DataView;

using Forma.Blazor.Components.Virtualization;

using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>The rows a virtualizing view draws over its data view: a heading for each group where the data view groups, and the items in rows of as many as stand across.</summary>
/// <typeparam name="TItem">The item the rows hold.</typeparam>
/// <remarks>
/// The rows are laid out as segments, one for an ungrouped set and one per group otherwise, each a heading row where it has a group
/// followed by its item rows unless the group is folded. A slice of rows is answered by fetching the item ranges those rows cover,
/// one fetch per run of consecutive items, so a folded group between two open ones is never read. Over a source that cannot
/// count, the rows run one slice past the last item answered until a slice comes back shorter than asked for, which ends the
/// set. The rows found by a fetch are published after the answer that found them, once the virtualization has taken it in;
/// a scroll the view asks for holds them back until it has landed. The rows also know the row holding the item whose marker last
/// took the focus and the rows of items either side of it, found among the rows last answered, for a view to pin.
/// </remarks>
/// <param name="view">The data view the rows are laid out over.</param>
/// <param name="virtualizer">The virtualizer the view draws the rows through, once it is drawn.</param>
/// <param name="invoke">Runs work on the view's renderer.</param>
/// <param name="redraw">Draws the view again.</param>
internal sealed class DataViewRows<TItem>(
    IDataView<TItem> view,
    Func<FormaVirtualize<DataViewRow<TItem>>?> virtualizer,
    Func<Func<Task>, Task> invoke,
    Action redraw)
    where TItem : notnull
{
    // Room enough for any set a source cannot count, which is read until it comes back short.
    private const int UncountedItemCount = int.MaxValue / 2;

    private readonly IDataView<TItem> _view = view;
    private readonly RowComparer _comparer = new(view.ItemComparer);
    private List<Segment> _segments = [];
    private List<(object? Key, int Count, bool IsFolded)> _shape = [];
    private int? _shapeTotal;
    private int _shapeColumnCount;
    private bool _isLaidOut;
    private int _columnCount = 1;
    private int _knownItemCount;
    private bool _isExhausted;
    private int _publishedRowCount;
    private int _continuationRowCount;
    private bool _isHeld;
    private List<DataViewRow<TItem>> _answeredRows = [];
    private object? _focusedItemKey;
    private DataViewRow<TItem>? _focusedRow;
    private DataViewRow<TItem>? _previousRow;
    private DataViewRow<TItem>? _nextRow;

    /// <summary>How many items stand across a row.</summary>
    public int ColumnCount
    {
        get => _columnCount;
        set => _columnCount = Math.Max(1, value);
    }

    /// <summary>How many rows the set lays out into.</summary>
    public int RowCount { get; private set; }

    /// <summary>Compares rows by the group whose heading they are or the item they open on.</summary>
    public IEqualityComparer<DataViewRow<TItem>> Comparer
        => _comparer;

    /// <summary>The stable key a row is drawn under: its group's key for a heading, its first item's key otherwise.</summary>
    /// <param name="row">The row.</param>
    public object GetRowKey(DataViewRow<TItem> row)
        => row.Group is { } group ? group.Key! : _view.ItemKey(row.Items[0]);

    /// <summary>The keys of the rows kept drawn wherever the view scrolls: the row whose marker last held the focus, so Tab finds
    /// it and the focus never falls to the page, and the rows of items either side of it, so an arrow from it reaches the one beside it.</summary>
    public HashSet<object> GetPinnedKeys()
    {
        HashSet<object> keys = [];

        // Until a row holding it is known, the focused item stands for its own row, which it heads in a view of one item a row.
        if (_focusedRow is { } focusedRow)
        {
            keys.Add(GetRowKey(focusedRow));
        }
        else if (_focusedItemKey is { } key)
        {
            keys.Add(key);
        }

        foreach (var row in (ReadOnlySpan<DataViewRow<TItem>?>)[_previousRow, _nextRow])
        {
            if (row is not null)
            {
                keys.Add(GetRowKey(row));
            }
        }

        return keys;
    }

    /// <summary>Notes the item whose marker took the focus, finding the rows either side of its own among the rows last answered.</summary>
    /// <param name="item">The item.</param>
    /// <returns>Whether the rows kept drawn changed.</returns>
    /// <remarks>An item in a row already kept, the focused row or one beside it, keeps the rows known around it, which the rows
    /// last answered may not hold: a marker taking the focus again, as it does when the window is returned to, and an arrow
    /// moving the focus along the focused row or onto a row beside it.</remarks>
    public bool NoteFocus(TItem item)
    {
        var pinnedKeys = GetPinnedKeys();
        var key = _view.ItemKey(item);

        if (!Equals(key, _focusedItemKey))
        {
            _focusedItemKey = key;
            (_previousRow, _focusedRow, _nextRow) = Holds(_focusedRow, key) ? (_previousRow, _focusedRow, _nextRow)
                : Holds(_nextRow, key) ? (_focusedRow, _nextRow, null)
                : Holds(_previousRow, key) ? (null, _previousRow, _focusedRow)
                : (null, null, null);
        }

        FindFocusedNeighbors();

        return !pinnedKeys.SetEquals(GetPinnedKeys());
    }

    /// <summary>Forgets what the rows were laid out from and what a source that cannot count has answered.</summary>
    public void Reset()
    {
        _isLaidOut = false;
        _knownItemCount = 0;
        _isExhausted = false;
        _publishedRowCount = 0;
        _continuationRowCount = 0;
    }

    /// <summary>Lays the rows out again.</summary>
    /// <returns>Whether the rows changed.</returns>
    public bool Update()
    {
        List<(object? Key, int Count, bool IsFolded)> shape = [.. _view.Groups.Select(group => (group.Key, GetLaidOutCount(group), _view.IsFolded(group)))];

        if (_isLaidOut && _shapeTotal == _view.ItemCount && _shapeColumnCount == _columnCount && shape.SequenceEqual(_shape))
        {
            return false;
        }

        _isLaidOut = true;
        _shape = shape;
        _shapeTotal = _view.ItemCount;
        _shapeColumnCount = _columnCount;
        _segments = [];
        var row = 0;

        if (_view.IsGrouped)
        {
            var firstItem = 0;

            foreach (var group in _view.Groups)
            {
                var segment = new Segment(row, group, firstItem, GetLaidOutCount(group), _view.IsFolded(group));
                _segments.Add(segment);
                row += segment.GetRowCount(_columnCount);
                firstItem += GetLaidOutCount(group);
            }
        }
        else if (_view.ItemCount is { } total and > 0)
        {
            var segment = new Segment(0, null, 0, total, false);
            _segments.Add(segment);
            row = segment.GetRowCount(_columnCount);
        }
        else if (_view.ItemCount is null)
        {
            // A source that cannot count is read as far as it answers; the rows end where a slice comes back short.
            var segment = new Segment(0, null, 0, UncountedItemCount, false);
            _segments.Add(segment);
            row = segment.GetRowCount(_columnCount);
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
                var itemRow = Math.Max(0, itemIndex - segment.FirstItem) / _columnCount;

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
            var firstItem = segment.FirstItem + (itemRow * _columnCount);
            rows.Add((null, firstItem, Math.Min(_columnCount, segment.FirstItem + segment.ItemCount - firstItem)));
        }

        var items = await FetchAsync(rows, request.CancellationToken);
        List<DataViewRow<TItem>> answeredRows = [.. rows.Select(row => new DataViewRow<TItem>(row.Group, row.Group is null ? Slice(items, row.FirstItem, row.ItemCount) : [], row.FirstItem))];

        if (_view.ItemCount is not null || _view.IsGrouped)
        {
            return Answer(answeredRows, RowCount);
        }

        // Uncounted, a slice shorter than asked for ends the set; until one does, the rows run one slice past the last answered,
        // so a scroll to their end asks for the next.
        var filledRows = answeredRows.TakeWhile(row => row.Items.Count > 0).ToList();

        if (filledRows.Count > 0)
        {
            _knownItemCount = Math.Max(_knownItemCount, filledRows[^1].FirstItemIndex + filledRows[^1].Items.Count);
        }

        _isExhausted |= filledRows.Count < rows.Count || (filledRows.Count > 0 && filledRows[^1].Items.Count < _columnCount);
        _continuationRowCount = Math.Max(_continuationRowCount, request.Count);

        // The first answer is published with it; a later one that found more rows is published once it has been drawn.
        if (_publishedRowCount == 0)
        {
            Publish();
        }
        else if (!_isHeld && PublishableRowCount != _publishedRowCount)
        {
            _ = invoke(RepublishAsync);
        }

        return Answer(filledRows, _publishedRowCount);
    }

    // A group lays out the items it holds where it holds them, as an appending view's do, and its whole count where it holds none.
    private static int GetLaidOutCount(DataViewGroup<TItem> group)
        => group.Items.Count > 0 ? group.Items.Count : group.Count;

    // The rows answered are kept to find the focused row's neighbors in; the view draws again where those changed, since it
    // hands the pinned keys to the virtualization as it draws.
    private ItemsProviderResult<DataViewRow<TItem>> Answer(List<DataViewRow<TItem>> rows, int count)
    {
        _answeredRows = rows;

        if (FindFocusedNeighbors())
        {
            redraw();
        }

        return new ItemsProviderResult<DataViewRow<TItem>>(rows, count);
    }

    // The row holding the focused item and the rows of items either side of it, headings passed over, read from the rows last
    // answered where they hold it; a side the answer ends before is left as it was. Returns whether the rows kept drawn changed.
    private bool FindFocusedNeighbors()
    {
        if (_focusedItemKey is not { } focusedItemKey)
        {
            return false;
        }

        var index = _answeredRows.FindIndex(row => Holds(row, focusedItemKey));

        if (index < 0)
        {
            return false;
        }

        var pinnedKeys = GetPinnedKeys();
        _focusedRow = _answeredRows[index];
        _previousRow = _answeredRows.Take(index).LastOrDefault(row => row.Group is null) ?? _previousRow;
        _nextRow = _answeredRows.Skip(index + 1).FirstOrDefault(row => row.Group is null) ?? _nextRow;

        return !pinnedKeys.SetEquals(GetPinnedKeys());
    }

    private bool Holds(DataViewRow<TItem>? row, object key)
        => row is not null && row.Items.Any(item => Equals(_view.ItemKey(item), key));

    /// <summary>Scrolls the virtualization to the row holding the item at <paramref name="itemIndex"/>.</summary>
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

            await rows.ScrollToIndexAsync(GetRowIndex(itemIndex), cancellationToken: cancellationToken);
        }
        finally
        {
            _isHeld = false;
        }

        await RepublishAsync();
    }

    // Uncounted, the rows only ever grow: every item answered so far, and one slice more while the source may hold further ones.
    private int PublishableRowCount
        => ((_knownItemCount + _columnCount - 1) / _columnCount) + (_isExhausted ? 0 : _continuationRowCount);

    private bool IsUncounted
        => _view.ItemCount is null && !_view.IsGrouped;

    private bool Publish()
    {
        if (!IsUncounted || PublishableRowCount == _publishedRowCount)
        {
            return false;
        }

        _publishedRowCount = PublishableRowCount;

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

    /// <summary>The position in the set of the first or the last item the rows reach, across the open groups.</summary>
    /// <param name="isLast">Whether the last is asked for rather than the first.</param>
    public int? GetEdgePosition(bool isLast)
    {
        if (_view.ItemCount is null && !_view.IsGrouped)
        {
            return _knownItemCount == 0 ? null : isLast ? _knownItemCount - 1 : 0;
        }

        var openSegments = _segments.Where(segment => !segment.IsFolded && segment.ItemCount > 0).ToList();

        return openSegments.Count == 0 ? null
            : isLast ? openSegments[^1].FirstItem + openSegments[^1].ItemCount - 1
            : openSegments[0].FirstItem;
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
        var lowIndex = 0;
        var highIndex = _segments.Count - 1;

        while (lowIndex < highIndex)
        {
            var middleIndex = (lowIndex + highIndex + 1) / 2;

            if (_segments[middleIndex].FirstRow <= row)
            {
                lowIndex = middleIndex;
            }
            else
            {
                highIndex = middleIndex - 1;
            }
        }

        return _segments[lowIndex];
    }

    /// <summary>A run of rows: an ungrouped set's items, or one group's heading followed by its items unless it is folded.</summary>
    private readonly record struct Segment(int FirstRow, DataViewGroup<TItem>? Group, int FirstItem, int ItemCount, bool IsFolded)
    {
        public int GetRowCount(int columnCount)
            => (Group is null ? 0 : 1) + (IsFolded ? 0 : (ItemCount + columnCount - 1) / columnCount);
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
                { Items: [var firstItem, ..] } => items.GetHashCode(firstItem),
                _ => 0,
            };
    }
}
