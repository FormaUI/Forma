namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>The rows a virtualizing view draws over its data view: a heading for each group where the data view groups, and the items in rows of as many as stand across.</summary>
/// <typeparam name="TItem">The item the rows hold.</typeparam>
/// <remarks>
/// The rows are laid out as segments, one for an ungrouped set and one per group otherwise, each a heading row where it has a group
/// followed by its item rows unless the group is folded. A window of rows is answered by fetching the item ranges those rows cover,
/// one fetch per run of consecutive items, so a folded group between two open ones is never read.
/// </remarks>
internal sealed class DataViewRows<TItem>(IDataView<TItem> view)
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

    /// <summary>Lays the rows out again from the data view's count, groups and folds and the columns across; true when the rows changed, so the view fetches its windows again.</summary>
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
            // A source that cannot count is read as far as it answers; the rows end where a window comes back short.
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

        // Uncounted, the set ends at the first row the source left empty.
        var filled = answered.TakeWhile(row => row.Items.Count > 0).ToList();

        return new ItemsProviderResult<DataViewRow<TItem>>(filled, request.StartIndex + filled.Count);
    }

    // The items the rows hold, read in one window per run of consecutive items and kept by their index in the set.
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

            var window = await _view.LoadWindowAsync(start, stop - start, cancellationToken);

            for (var offset = 0; offset < window.Items.Count; offset++)
            {
                items[start + offset] = window.Items[offset];
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
