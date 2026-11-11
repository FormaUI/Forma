namespace Forma.Blazor.Components.Virtualization;

using System.Globalization;

using Forma.Framework.Browser.Interop.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

/// <summary>A long list rendered around the viewport, with the items its owner pins.</summary>
/// <typeparam name="TItem">The item the list holds.</typeparam>
/// <remarks>
/// The items and the spacers are drawn side by side under the owner's element, in the items' order: a spacer opening the list, the
/// rendered range of items around the viewport and each pinned item outside it, a spacer standing for each run of items between them, and a
/// spacer closing the list. Every item is keyed by its owner's key, so one that stays drawn, in the rendered range or pinned, keeps its element
/// as the ranges around it change. A spacer is as tall as the items it stands for at the average room a drawn item takes, from its top to the next item's.
/// Over a provider, the item a fetch opens on is tracked from one answer to the next; where the item there has changed, the set was added to or
/// taken from before it, and the viewport holds its place unless <see cref="AnchorMode"/> names the edge it stands at and it is already there,
/// where it follows what arrived instead.
/// </remarks>
public sealed partial class FormaVirtualize<TItem> : FormaComponentBase
{
    private const string SpacerAttribute = "data-virtualize-spacer";
    private const string StartRole = "start";
    private const string GapRole = "gap";
    private const string EndRole = "end";

    // How many times a scroll to an item draws the list again before it finds the item drawn: once to reach it, and once more
    // where the heights the reach measured moved it.
    private const int ScrollAttemptCount = 3;

    /// <summary>The items the list holds.</summary>
    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>The provider the items are read from, a slice at a time.</summary>
    [Parameter] public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    /// <summary>The stable key of an item.</summary>
    [Parameter, EditorRequired] public Func<TItem, object> ItemKey { get; set; } = default!;

    /// <summary>The element an item draws.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> ItemContent { get; set; } = default!;

    /// <summary>The element drawn for an item the provider has not answered yet.</summary>
    [Parameter] public RenderFragment<PlaceholderContext>? Placeholder { get; set; }

    /// <summary>The estimated room an item takes, in pixels.</summary>
    [Parameter] public float ItemSize { get; set; } = 50;

    /// <summary>How many items are drawn beyond each edge of the viewport.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>The element a spacer is drawn as.</summary>
    [Parameter] public string SpacerElement { get; set; } = "div";

    /// <summary>The keys of the items kept rendered wherever they lie.</summary>
    [Parameter] public IReadOnlyCollection<object>? PinnedKeys { get; set; }

    /// <summary>The edge the viewport follows when a provider's set grows there while it already stands at it.</summary>
    [Parameter] public VirtualizeAnchorMode AnchorMode { get; set; } = VirtualizeAnchorMode.Start;

    /// <summary>Compares two items a provider answered, to tell the set added to from the same set fetched again.</summary>
    [Parameter] public IEqualityComparer<TItem> ItemComparer { get; set; } = EqualityComparer<TItem>.Default;

    /// <summary>The item the list opens scrolled to.</summary>
    [Parameter] public int InitialItemIndex { get; set; }

    private readonly Dictionary<object, (int Index, TItem Item)> _pins = [];

    // The items read around the pins when the items were last read again, each run with the position it starts at.
    private List<(int Start, List<TItem> Items)> _pinSurroundings = [];
    private Virtualize _interop = default!;
    private DotNetObjectReference<FormaVirtualize<TItem>>? _selfReference;
    private ElementReference _startSpacer;
    private bool _isAttached;
    private HashSet<object> _pinnedKeys = [];
    private IReadOnlyList<TItem>? _indexedItems;
    private Dictionary<object, int>? _indexByKey;
    private double _offset;
    private double _viewport;
    private double _measuredItemSize;
    private int _count;
    private bool _hasLoaded;
    private bool _isStale;
    private int _loadedStart;
    private List<TItem> _loadedItems = [];
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _scrollCancellation;
    private List<TaskCompletionSource> _renderWaiters = [];
    private List<Drawn> _drawn = [];
    private (int Index, TItem Item)? _loadedAnchor;
    private bool _hasAppliedInitialIndex;
    private bool _initialIndexNeedsCorrection;

    private enum DrawnKind
    {
        Spacer,
        Item,
        Placeholder,
    }

    /// <summary>How many items the list holds.</summary>
    public int Count
        => _count;

    // The room every spacer counts an item at: the average the drawn items take, or the estimate before any are drawn.
    private double CurrentItemSize
        => _measuredItemSize > 0 ? _measuredItemSize : ItemSize;

    // How many items the list draws before it knows the viewport it stands in.
    private int InitialCount
        => (OverscanCount * 2) + 1;

    /// <summary>Scrolls the item at <paramref name="index"/> into view.</summary>
    /// <param name="index">The item's position in the list.</param>
    /// <param name="alignment">Where the scroll brings the item.</param>
    /// <param name="cancellationToken">Cancels the scroll.</param>
    /// <returns>Whether the item is in view.</returns>
    public async Task<bool> ScrollToIndexAsync(int index, ScrollAlignment alignment = ScrollAlignment.Nearest, CancellationToken cancellationToken = default)
    {
        if (_scrollCancellation is { } supersededCancellation)
        {
            await supersededCancellation.CancelAsync();
        }

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = cancellation.Token;
        _scrollCancellation = cancellation;

        try
        {
            for (var attempt = 0; attempt < ScrollAttemptCount; attempt++)
            {
                token.ThrowIfCancellationRequested();

                if (index < 0 || index >= _count)
                {
                    return false;
                }

                if (GetOrdinal(index) is { } ordinal)
                {
                    return await _interop.RevealAsync(ordinal, alignment == ScrollAlignment.Start);
                }

                // Not drawn: the rendered range around where the item stands is drawn, and the list scrolls there once it is, so the
                // viewport never stands over spacers alone, which WebKit anchors to as their heights change.
                var top = index * CurrentItemSize;
                var target = alignment == ScrollAlignment.Start || top < _offset ? top : top - Math.Max(0, _viewport - CurrentItemSize);
                _offset = target;
                await LoadAsync();
                await RenderAsync(token);
                await _interop.ScrollToAsync(target);
            }

            return false;
        }
        finally
        {
            if (ReferenceEquals(_scrollCancellation, cancellation))
            {
                _scrollCancellation = null;
            }
        }
    }

    /// <summary>Reads the items again.</summary>
    public async Task RefreshDataAsync()
    {
        _isStale = true;
        _indexByKey = null;

        if (Items is { } items)
        {
            _count = items.Count;
        }

        await LoadAsync();
        StateHasChanged();
    }

    /// <summary>Takes where the list stands in the viewport.</summary>
    /// <param name="offset">How far the top of the viewport stands past the start of the list, in pixels.</param>
    /// <param name="viewport">How tall the viewport is, in pixels.</param>
    /// <param name="itemSize">The average room a drawn item takes, in pixels.</param>
    [JSInvokable]
    public async Task HandleViewportAsync(double offset, double viewport, double itemSize)
    {
        var range = GetRenderedRange();
        var size = CurrentItemSize;
        _offset = offset;
        _viewport = viewport;

        if (itemSize > 0)
        {
            _measuredItemSize = itemSize;
        }

        // The item the list opened on was placed by the estimated size, before any item had been measured; the first real
        // measurement corrects it to where the item actually stands.
        if (_initialIndexNeedsCorrection && itemSize > 0)
        {
            _initialIndexNeedsCorrection = false;
            var correctedOffset = InitialItemIndex * CurrentItemSize;

            if (Math.Abs(correctedOffset - _offset) >= 1)
            {
                _offset = correctedOffset;
                await LoadAsync();
                StateHasChanged();
                await _interop.ScrollToAsync(_offset);
                return;
            }
        }

        if (GetRenderedRange() == range && Math.Abs(CurrentItemSize - size) < 0.5)
        {
            return;
        }

        await LoadAsync();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
        => _interop = new Virtualize(Context.JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if ((Items is null) == (ItemsProvider is null))
        {
            throw new InvalidOperationException($"{nameof(FormaVirtualize<>)} takes {nameof(Items)} or {nameof(ItemsProvider)}, one of the two.");
        }

        ArgumentNullException.ThrowIfNull(ItemKey);
        ArgumentNullException.ThrowIfNull(ItemContent);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ItemSize);
        ArgumentOutOfRangeException.ThrowIfNegative(OverscanCount);
        ArgumentOutOfRangeException.ThrowIfNegative(InitialItemIndex);
        ArgumentException.ThrowIfNullOrWhiteSpace(SpacerElement);
        ArgumentNullException.ThrowIfNull(ItemComparer);

        if (!ReferenceEquals(Items, _indexedItems))
        {
            _indexedItems = Items;
            _indexByKey = null;
        }

        if (Items is { } items)
        {
            _count = items.Count;
        }

        _pinnedKeys = PinnedKeys is { Count: > 0 } keys ? [.. keys] : [];

        // What a pin kept of an item goes with the pin.
        foreach (var key in _pins.Keys.Where(key => !_pinnedKeys.Contains(key)).ToList())
        {
            _pins.Remove(key);
        }

        // A key pinned from an item already answered is recorded now, over a provider: the fetch that answered it ran before it
        // was pinned, and no fetch after it is bound to ask for its position again.
        if (ItemsProvider is not null)
        {
            foreach (var (start, around) in _pinSurroundings)
            {
                RecordPins(start, around);
            }

            RecordPins(_loadedStart, _loadedItems);
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (ItemsProvider is not null && !_hasLoaded)
        {
            await LoadAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isAttached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);

            // Claimed before the await, so a render arriving mid-call attaches no second observer.
            _isAttached = true;
            _isAttached = await _interop.AttachAsync(_startSpacer, _selfReference, SpacerAttribute);

            // Opens on the item by its own offset rather than through ScrollToIndexAsync, which would wait on a render of its
            // own to confirm the item drawn — a render this very call is the one to produce, so waiting on it here would wait
            // on itself. The range around the new offset is drawn on the render StateHasChanged asks for; the real scroll is a
            // plain interop call with no render of its own to wait on, so it is awaited directly.
            if (_isAttached && InitialItemIndex > 0 && !_hasAppliedInitialIndex)
            {
                _hasAppliedInitialIndex = true;
                _initialIndexNeedsCorrection = true;
                _offset = InitialItemIndex * CurrentItemSize;
                await LoadAsync();
                StateHasChanged();
                await _interop.ScrollToAsync(_offset);
            }
        }

        var waiters = _renderWaiters;
        _renderWaiters = [];

        foreach (var waiter in waiters)
        {
            waiter.TrySetResult();
        }
    }

    // The rendered range: the items around the viewport and the overscan beyond it, from the first to the one past the last; pinned items stand outside it.
    private (int FirstIndex, int End) GetRenderedRange()
    {
        var size = CurrentItemSize;

        // Before the viewport is known, the range opens at the offset asked for, which is the start until a scroll asks for another.
        if (_viewport <= 0)
        {
            var start = Math.Clamp((int)Math.Floor(Math.Max(0, _offset) / size), 0, _count);

            return (start, Math.Min(start + InitialCount, _count));
        }

        var top = Math.Max(0, _offset);
        var bottom = Math.Max(top, _offset + _viewport);
        var firstIndex = Math.Clamp((int)Math.Floor(top / size) - OverscanCount, 0, _count);
        var end = Math.Clamp((int)Math.Ceiling(bottom / size) + OverscanCount, firstIndex, _count);

        return (firstIndex, end);
    }

    // What the list draws, in order: the spacers, and the rendered range's items and the pinned ones, each once.
    private List<Drawn> Draw()
    {
        var (firstIndex, end) = GetRenderedRange();
        SortedDictionary<int, Drawn> entries = [];

        for (var index = firstIndex; index < end; index++)
        {
            if (TryGetItem(index, out var item))
            {
                entries[index] = new Drawn(DrawnKind.Item, index, item, 0, null);
            }
            else if (Placeholder is not null)
            {
                entries[index] = new Drawn(DrawnKind.Placeholder, index, default, 0, null);
            }
        }

        foreach (var (index, item) in GetPinned())
        {
            entries[index] = new Drawn(DrawnKind.Item, index, item, 0, null);
        }

        var size = CurrentItemSize;
        List<Drawn> drawn = [];
        var cursor = 0;

        foreach (var (index, entry) in entries)
        {
            if (drawn.Count == 0)
            {
                drawn.Add(Spacer(StartRole, 0, index * size));
            }
            else if (index > cursor)
            {
                // A gap is keyed by the first position it stands for, which holds while the runs after it move, so a gap above the
                // viewport keeps its element: WebKit moves the viewport when a spacer above it is replaced by another.
                drawn.Add(Spacer(GapRole, cursor, (index - cursor) * size));
            }

            drawn.Add(entry);
            cursor = index + 1;
        }

        if (drawn.Count == 0)
        {
            drawn.Add(Spacer(StartRole, 0, _count * size));
            cursor = _count;
        }

        drawn.Add(Spacer(EndRole, _count, Math.Max(0, _count - cursor) * size));
        _drawn = drawn;

        return drawn;
    }

    private static Drawn Spacer(string role, int position, double height)
        => new(DrawnKind.Spacer, position, default, height, role);

    // The spacers and the items as keyed siblings of one sequence.
    private RenderFragment RenderList() => builder =>
    {
        foreach (var drawn in Draw())
        {
            switch (drawn.Kind)
            {
                case DrawnKind.Spacer:
                    builder.OpenElement(0, SpacerElement);
                    builder.SetKey(new SpacerKey(drawn.Role!, drawn.Index));
                    builder.AddAttribute(1, SpacerAttribute, drawn.Role);
                    builder.AddAttribute(2, "aria-hidden", "true");
                    builder.AddAttribute(3, "style", string.Create(CultureInfo.InvariantCulture, $"block-size: {drawn.Height:0.##}px; flex-shrink: 0; overflow-anchor: none"));

                    if (drawn.Role == StartRole)
                    {
                        builder.AddElementReferenceCapture(4, reference => _startSpacer = reference);
                    }

                    builder.CloseElement();
                    break;
                case DrawnKind.Item:
                    builder.OpenComponent<FormaVirtualizeItem>(5);
                    builder.SetKey(ItemKey(drawn.Item!));
                    builder.AddComponentParameter(6, nameof(FormaVirtualizeItem.Content), ItemContent(drawn.Item!));
                    builder.CloseComponent();
                    break;
                default:
                    builder.OpenComponent<FormaVirtualizeItem>(7);
                    builder.SetKey(new PlaceholderKey(drawn.Index));
                    builder.AddComponentParameter(8, nameof(FormaVirtualizeItem.Content), Placeholder!(new PlaceholderContext(drawn.Index, (float)CurrentItemSize)));
                    builder.CloseComponent();
                    break;
            }
        }
    };

    // An item's place among the items the list draws, spacers left out.
    private int? GetOrdinal(int index)
    {
        var ordinal = 0;

        foreach (var drawn in _drawn)
        {
            if (drawn.Kind == DrawnKind.Spacer)
            {
                continue;
            }

            if (drawn.Index == index)
            {
                return drawn.Kind == DrawnKind.Item ? ordinal : null;
            }

            ordinal++;
        }

        return null;
    }

    private bool TryGetItem(int index, out TItem item)
    {
        if (Items is { } items)
        {
            item = items[index];
            return true;
        }

        var offset = index - _loadedStart;

        if (_hasLoaded && offset >= 0 && offset < _loadedItems.Count)
        {
            item = _loadedItems[offset];
            return true;
        }

        item = default!;
        return false;
    }

    // The pinned items the list can find: in the items the owner holds, or among those a provider has answered.
    private IEnumerable<(int Index, TItem Item)> GetPinned()
    {
        if (_pinnedKeys.Count == 0)
        {
            yield break;
        }

        if (Items is { } items)
        {
            _indexByKey ??= IndexByKey(items);

            foreach (var key in _pinnedKeys)
            {
                if (_indexByKey.TryGetValue(key, out var index))
                {
                    yield return (index, items[index]);
                }
            }

            yield break;
        }

        foreach (var pin in _pins.Values)
        {
            if (pin.Index < _count)
            {
                yield return pin;
            }
        }
    }

    private Dictionary<object, int> IndexByKey(IReadOnlyList<TItem> items)
    {
        Dictionary<object, int> indexByKey = [];

        for (var index = 0; index < items.Count; index++)
        {
            indexByKey[ItemKey(items[index])] = index;
        }

        return indexByKey;
    }

    // A provider is asked for the rendered range once it has moved past what it answered, or once the items were read again.
    private async Task LoadAsync()
    {
        if (ItemsProvider is not { } provider)
        {
            return;
        }

        var (firstIndex, end) = _hasLoaded ? GetRenderedRange() : (0, InitialCount);

        if (_hasLoaded && !_isStale && firstIndex >= _loadedStart && end <= _loadedStart + _loadedItems.Count)
        {
            return;
        }

        if (_loadCancellation is { } supersededCancellation)
        {
            await supersededCancellation.CancelAsync();
            supersededCancellation.Dispose();
        }

        var cancellation = _loadCancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var wasStale = _isStale;
        var hadLoadedBefore = _hasLoaded;
        var previousCount = _count;
        var previousOffset = _offset;
        var previousViewport = _viewport;
        var isAnchored = false;
        ItemsProviderResult<TItem> result;
        List<TItem> loadedItems;

        try
        {
            result = await provider(new ItemsProviderRequest(firstIndex, Math.Max(1, end - firstIndex), token));
            loadedItems = [.. result.Items];

            var delta = result.TotalItemCount - previousCount;

            if (hadLoadedBefore && delta != 0
                && AdjustForAnchor(result.TotalItemCount, previousCount, delta, previousOffset, previousViewport, firstIndex, loadedItems) is { } adjustedOffset)
            {
                // The corrected range is computed against the set's new count, which lands early for that alone. The window
                // itself moves by exactly what changed before it rather than being re-read from the corrected offset: an
                // ancestor outside the list, a sticky header among them, can leave the offset negative at the true top, where
                // it is clamped to zero, so deriving the window from pixels again does not track the same shift linearly.
                isAnchored = true;
                _count = result.TotalItemCount;
                _offset = adjustedOffset;
                firstIndex = Math.Clamp(firstIndex + delta, 0, _count);
                end = Math.Clamp(end + delta, firstIndex, _count);
                result = await provider(new ItemsProviderRequest(firstIndex, Math.Max(1, end - firstIndex), token));
                loadedItems = [.. result.Items];
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            // The set stays stale until a read that was not superseded answers, so the read that supersedes one still reads it again.
            _isStale = false;
            _loadedStart = firstIndex;
            _loadedItems = loadedItems;
            _count = result.TotalItemCount;
            _hasLoaded = true;

            // A set that grew or shrank has moved what was read around the pins.
            if (_count != previousCount)
            {
                _pinSurroundings = [];
            }

            RecordPins(_loadedStart, _loadedItems);
            _loadedAnchor = _loadedItems.Count > 0 ? (_loadedStart, _loadedItems[0]) : null;

            if (wasStale)
            {
                await VerifyPinsAsync(provider, token);
            }

            // A held or followed viewport is drawn at its old position until this render lands, so the real scroll is corrected after it.
            if (isAnchored)
            {
                await RenderAsync(token);
                await _interop.ScrollToAsync(_offset);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    // The offset that holds the viewport in place or follows it to the edge it stands at, or null where nothing arrived to react to.
    // The item the last answer opened on is found in this one, at the same position, to tell the set added to from the set fetched
    // again: where it no longer matches, the set grew or shrank before it; where it still matches and the set grew, the set grew
    // after it.
    private double? AdjustForAnchor(int newCount, int previousCount, int delta, double previousOffset, double previousViewport, int firstIndex, List<TItem> loadedItems)
    {
        if (_loadedAnchor is not { } anchor || anchor.Index < firstIndex || anchor.Index - firstIndex >= loadedItems.Count)
        {
            return null;
        }

        var size = CurrentItemSize;

        if (!ItemComparer.Equals(anchor.Item, loadedItems[anchor.Index - firstIndex]))
        {
            // Added or taken before the anchor. Standing at the start already, the start shows what arrived there; otherwise the
            // viewport holds the items it showed.
            var atStart = previousOffset <= size;

            return (AnchorMode & VirtualizeAnchorMode.Start) != VirtualizeAnchorMode.None && atStart
                ? null
                : Math.Max(0, previousOffset + (delta * size));
        }

        // Added after the anchor, with the end already in view, is followed there; taken, or added with the end out of view, leaves
        // the viewport where it was.
        var atEnd = previousOffset + previousViewport >= (previousCount * size) - size;

        return delta > 0 && (AnchorMode & VirtualizeAnchorMode.End) != VirtualizeAnchorMode.None && atEnd
            ? Math.Max(0, (newCount * size) - previousViewport)
            : null;
    }

    // A pinned item a provider answered is kept with its position, so it can be drawn once the rendered range moves away from it.
    private void RecordPins(int start, List<TItem> items)
    {
        if (_pinnedKeys.Count == 0)
        {
            return;
        }

        for (var offset = 0; offset < items.Count; offset++)
        {
            var key = ItemKey(items[offset]);

            if (_pinnedKeys.Contains(key))
            {
                _pins[key] = (start + offset, items[offset]);
            }
        }
    }

    // After the items were read again, each pinned item is looked for in the rendered range, else among the items read around
    // where it stood, the overscan either side of it, and kept where it is found or let go. The items read around the pins are
    // kept, so a key the owner pins once it has seen them, an item beside a pinned one, is found among them.
    private async Task VerifyPinsAsync(ItemsProviderDelegate<TItem> provider, CancellationToken token)
    {
        _pinSurroundings = [];
        HashSet<object> foundKeys = [.. _loadedItems.Select(ItemKey).Where(_pinnedKeys.Contains)];

        foreach (var (key, (index, _)) in _pins.ToList())
        {
            if (foundKeys.Contains(key))
            {
                continue;
            }

            if (index < _count && (index < _loadedStart || index >= _loadedStart + _loadedItems.Count))
            {
                var start = Math.Max(0, index - OverscanCount);
                var result = await provider(new ItemsProviderRequest(start, Math.Min(_count, index + OverscanCount + 1) - start, token));

                if (token.IsCancellationRequested)
                {
                    return;
                }

                List<TItem> around = [.. result.Items];
                _pinSurroundings.Add((start, around));
                RecordPins(start, around);
                foundKeys.UnionWith(around.Select(ItemKey).Where(_pinnedKeys.Contains));
            }

            if (!foundKeys.Contains(key))
            {
                _pins.Remove(key);
            }
        }
    }

    // Draws the list again and waits until the draw is on the page.
    private Task RenderAsync(CancellationToken token)
    {
        var renderWaiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _renderWaiters.Add(renderWaiter);
        StateHasChanged();

        return renderWaiter.Task.WaitAsync(token);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_scrollCancellation is not null)
        {
            await _scrollCancellation.CancelAsync();
            _scrollCancellation.Dispose();
        }

        if (_loadCancellation is not null)
        {
            await _loadCancellation.CancelAsync();
            _loadCancellation.Dispose();
        }

        await _interop.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }

    /// <summary>One thing the list draws: a spacer, an item or a placeholder.</summary>
    private readonly record struct Drawn(DrawnKind Kind, int Index, TItem? Item, double Height, string? Role);

    /// <summary>The key of a spacer.</summary>
    private sealed record SpacerKey(string Role, int Position);

    /// <summary>The key of a placeholder.</summary>
    private sealed record PlaceholderKey(int Index);
}
