namespace Forma.Blazor.Components.Virtualization;

using System.Globalization;

using Forma.Framework.Browser.Interop.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

/// <summary>Renders the items of a long list near the viewport, with spacers standing for the rest, and keeps rendered the items its owner pins wherever they lie.</summary>
/// <typeparam name="TItem">The item the list holds.</typeparam>
/// <remarks>
/// The items and the spacers are drawn side by side under the owner's element, in the items' order: a spacer opening the list, the
/// rendered range of items around the viewport and each pinned item outside it, a spacer standing for each run of items between them, and a
/// spacer closing the list. Every item is keyed by its owner's key, so one that stays drawn, in the rendered range or pinned, keeps its element
/// as the ranges around it change. A spacer is as tall as the items it stands for at the average room a drawn item takes, from its top to the next item's.
/// </remarks>
public sealed partial class FormaVirtualize<TItem> : FormaComponentBase
{
    private const string SpacerAttribute = "data-virtualize-spacer";
    private const string StartRole = "start";
    private const string GapRole = "gap";
    private const string EndRole = "end";

    // How many times a scroll to an item draws the list again before it finds the item drawn: once to reach it, and once more
    // where the heights the reach measured moved it.
    private const int ScrollAttempts = 3;

    /// <summary>The items the owner holds. Supply this or <see cref="ItemsProvider"/>, not both.</summary>
    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Answers a slice of positions with its items and the size of the whole set, for items the owner does not hold. Supply this or <see cref="Items"/>, not both.</summary>
    [Parameter] public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    /// <summary>The stable key of an item, which keeps its element while it stays drawn and names it in <see cref="PinnedKeys"/>.</summary>
    [Parameter, EditorRequired] public Func<TItem, object> ItemKey { get; set; } = default!;

    /// <summary>The one element an item draws.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> ItemContent { get; set; } = default!;

    /// <summary>The one element drawn for an item a provider has not answered yet; a spacer stands for it where none is given.</summary>
    [Parameter] public RenderFragment<PlaceholderContext>? Placeholder { get; set; }

    /// <summary>The room an item is estimated to take, in pixels from its top to the next item's, until items have been drawn and measured.</summary>
    [Parameter] public float ItemSize { get; set; } = 50;

    /// <summary>How many items are drawn beyond each edge of the viewport.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>The element a spacer is, <c>div</c> in a list and <c>tr</c> among the rows of a table.</summary>
    [Parameter] public string SpacerElement { get; set; } = "div";

    /// <summary>The keys of the items kept drawn wherever they lie, such as the item that holds the focus; a key that names no item draws nothing.</summary>
    [Parameter] public IReadOnlyCollection<object>? PinnedKeys { get; set; }

    private readonly Dictionary<object, (int Index, TItem Item)> _pins = [];
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
    private List<TItem> _loaded = [];
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _scrollCancellation;
    private List<TaskCompletionSource> _renderWaiters = [];
    private List<Drawn> _drawn = [];

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

    /// <summary>Scrolls the item at <paramref name="index"/> into view, completing true once it is in view, and false when there is no item at the index or it could not be drawn; a newer scroll cancels it, as <paramref name="cancellationToken"/> does.</summary>
    /// <param name="index">The item's position in the list.</param>
    /// <param name="alignment">Whether the item is brought by the least scroll that shows it or to the top of the viewport.</param>
    /// <param name="cancellationToken">Canceled when the scroll is no longer wanted.</param>
    public async Task<bool> ScrollToIndexAsync(int index, ScrollAlignment alignment = ScrollAlignment.Nearest, CancellationToken cancellationToken = default)
    {
        if (_scrollCancellation is { } superseded)
        {
            await superseded.CancelAsync();
        }

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = cancellation.Token;
        _scrollCancellation = cancellation;

        try
        {
            for (var attempt = 0; attempt < ScrollAttempts; attempt++)
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

                // Not drawn: the list scrolls to where the item stands, which draws the rendered range around it.
                var top = index * CurrentItemSize;
                var target = alignment == ScrollAlignment.Start || top < _offset ? top : top - Math.Max(0, _viewport - CurrentItemSize);
                _offset = target;
                await _interop.ScrollToAsync(target);
                await LoadAsync();
                await RenderAsync(token);
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

    /// <summary>Reads the items again, for a list whose items changed: a provider is asked for the rendered range again, and a pinned item outside it is kept only while it still stands at its position.</summary>
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

    /// <summary>Takes where the list stands in the viewport, reported by the browser.</summary>
    /// <param name="offset">How far the top of the viewport stands past the start of the list, in pixels; negative while the list starts below it.</param>
    /// <param name="viewport">How tall the viewport is, in pixels.</param>
    /// <param name="itemSize">The average room a drawn item takes, from its top to the next item's, or zero where none are drawn.</param>
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
        ArgumentException.ThrowIfNullOrWhiteSpace(SpacerElement);

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
            _isAttached = await _interop.AttachAsync(_startSpacer, _selfReference, SpacerAttribute);
        }

        var waiters = _renderWaiters;
        _renderWaiters = [];

        foreach (var waiter in waiters)
        {
            waiter.TrySetResult();
        }
    }

    // The rendered range: the items around the viewport and the overscan beyond it, from the first to the one past the last; pinned items stand outside it.
    private (int First, int End) GetRenderedRange()
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
        var first = Math.Clamp((int)Math.Floor(top / size) - OverscanCount, 0, _count);
        var end = Math.Clamp((int)Math.Ceiling(bottom / size) + OverscanCount, first, _count);

        return (first, end);
    }

    // What the list draws, in order: the spacers, and the rendered range's items and the pinned ones, each once.
    private List<Drawn> Draw()
    {
        var (first, end) = GetRenderedRange();
        SortedDictionary<int, Drawn> entries = [];

        for (var index = first; index < end; index++)
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
                drawn.Add(Spacer(GapRole, index, (index - cursor) * size));
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

    private static Drawn Spacer(string role, int before, double height)
        => new(DrawnKind.Spacer, before, default, height, role);

    // The spacers and the items as siblings of one sequence, each keyed, which is what lets an item that stays drawn keep its element.
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

    // An item's place among the items the list draws, spacers left out, or null where it is not drawn.
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

        if (_hasLoaded && offset >= 0 && offset < _loaded.Count)
        {
            item = _loaded[offset];
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

        var (first, end) = _hasLoaded ? GetRenderedRange() : (0, InitialCount);

        if (_hasLoaded && !_isStale && first >= _loadedStart && end <= _loadedStart + _loaded.Count)
        {
            return;
        }

        if (_loadCancellation is { } superseded)
        {
            await superseded.CancelAsync();
            superseded.Dispose();
        }

        var cancellation = _loadCancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var wasStale = _isStale;
        ItemsProviderResult<TItem> result;

        try
        {
            result = await provider(new ItemsProviderRequest(first, Math.Max(1, end - first), token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        // The set stays stale until a read that was not superseded answers, so the read that supersedes one still reads it again.
        _isStale = false;
        _loadedStart = first;
        _loaded = [.. result.Items];
        _count = result.TotalItemCount;
        _hasLoaded = true;
        RecordPins(_loadedStart, _loaded);

        if (wasStale)
        {
            await VerifyPinsAsync(provider, token);
        }
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

    // After the items were read again, a pinned item outside the rendered range is kept only while its position still holds it.
    private async Task VerifyPinsAsync(ItemsProviderDelegate<TItem> provider, CancellationToken token)
    {
        foreach (var (key, (index, _)) in _pins.ToList())
        {
            if (index >= _loadedStart && index < _loadedStart + _loaded.Count)
            {
                continue;
            }

            var result = index < _count ? await provider(new ItemsProviderRequest(index, 1, token)) : default;

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (!_pinnedKeys.Contains(key))
            {
                continue;
            }

            if (result.Items is { } answered && answered.Take(1).ToList() is [var item] && Equals(ItemKey(item), key))
            {
                _pins[key] = (index, item);
            }
            else
            {
                _pins.Remove(key);
            }
        }
    }

    // Draws the list again and waits until the draw is on the page.
    private Task RenderAsync(CancellationToken token)
    {
        var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _renderWaiters.Add(rendered);
        StateHasChanged();

        return rendered.Task.WaitAsync(token);
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

    /// <summary>One thing the list draws: a spacer standing for a run of items, an item, or a placeholder for an item not yet answered.</summary>
    private readonly record struct Drawn(DrawnKind Kind, int Index, TItem? Item, double Height, string? Role);

    /// <summary>Keys a spacer by its role and the item it stands before, apart from every item's key.</summary>
    private sealed record SpacerKey(string Role, int Before);

    /// <summary>Keys a placeholder by the position it stands for, apart from every item's key.</summary>
    private sealed record PlaceholderKey(int Index);
}
