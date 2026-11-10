namespace Forma.Blazor.Components.DataView;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Blazor.Components.Inputs;
using Forma.Blazor.Components.Virtualization;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Click;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A view over a data view drawing the hierarchy as nodes under their parents, each opened, closed and selected from the keyboard, with a busy state while its children load.</summary>
/// <typeparam name="TItem">The item a node shows.</typeparam>
public sealed partial class FormaDataTree<TItem> : FormaElementBase, IDataViewViewport, IDataViewPositions<TItem>
    where TItem : notnull
{
    private const string ViewKey = "tree";

    // What the tree is given that is not its own: the host's attributes, which never change how a node is drawn.
    private static readonly string[] _parametersLeftOut = [nameof(Id), nameof(Class), nameof(Style)];
    private const string NextAction = "tree.next";
    private const string PreviousAction = "tree.previous";
    private const string FirstAction = "tree.first";
    private const string LastAction = "tree.last";
    private const string OpenAction = "tree.open";
    private const string CloseAction = "tree.close";
    private const string ChooseAction = "tree.choose";
    private const string FindAction = "tree.find";

    // The attribute marking the element that takes the focus from a node that went away.
    private const string HeirAttribute = "data-focus-heir";

    /// <summary>What a node shows beside its name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The item's depiction drawn before its name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ThumbnailTemplate { get; set; }

    /// <summary>Whether the rows tighten to the line of a node's name.</summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>Whether a chosen node becomes the current node, whose children the other views show, with the tree standing beside them as a sidebar.</summary>
    [Parameter] public bool SelectsCurrent { get; set; }

    /// <summary>The filter admitting the items the tree shows, where null admits every item.</summary>
    [Parameter] public Func<TItem, bool>? NodeFilter { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    private CollectionName _collectionName = CollectionName.None;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private DataViewRegistration _dataViewRegistration = default!;
    private readonly Dictionary<object, ElementReference> _nodeElements = [];
    private readonly Dictionary<object, FormaCheckBox> _nodeMarkers = [];
    private readonly ParameterRevision _parameterRevision = new(_parametersLeftOut);
    private InputRegistration _inputRegistration = default!;
    private InputProfile? _inputProfile;
    private bool _profileKeepsFocus;
    private bool _profileIsRightToLeft;
    private TItem? _focusedNode;

    // The nodes the tree has moved the focus to, in order, whose arrival the browser has yet to report.
    private readonly List<object> _unreportedMoves = [];
    private bool _isCompanion;
    private bool _areRootsRequested;
    private object? _tabStopKey;
    private object? _pendingFocusKey;
    private object? _lastFocusedKey;
    private bool _holdsFocus;
    private bool _focusesSelf;
    private object? _heirKey;
    private static readonly FocusInteractionOptions RecoveryOptions = new() { HeirAttribute = HeirAttribute };

    // Each node carries its token, which names the node holding the focus.
    private static readonly FocusWatchOptions FocusOptions = new() { ItemAttribute = "data-tree-node" };

    private DomInteraction<FocusInteractionOptions>? _recovery;
    private Task<DomObservation<FocusWatchOptions, FocusState>>? _focusWatching;
    private DomObservation<FocusWatchOptions, FocusState>? _focusState;
    private FormaVirtualize<VisibleNode>? _virtualizer;
    private List<VisibleNode> _visibleRows = [];

    /// <summary>The element each node drew, by the node's key, which the focus is given to.</summary>
    internal Dictionary<object, ElementReference> NodeElements
        => _nodeElements;

    /// <summary>The box each node drew, by the node's key, which the focus is given to where boxes carry it.</summary>
    internal Dictionary<object, FormaCheckBox> NodeMarkers
        => _nodeMarkers;

    // What every node is drawn by: it changes when the data view or the tree's own parameters changed in a way a node draws differently for.
    private int NodeRevision
        => View.Revision + _parameterRevision.Value;

    private bool IsActive
        => _isCompanion || View.ActiveViewKey == ViewKey;

    // A sidebar's choice is the current node, so its rows carry no marker of the data view's selection.
    internal bool HasSelection
        => !SelectsCurrent && (View.EnableCheckedSelection || View.EnableItemSelection);

    // Without a marker to carry the focus, the rows carry it themselves, one of them the tab stop.
    private bool KeepsFocus
        => !HasSelection;

    private bool IsVirtualizing
        => View.IsVirtualizing;

    /// <inheritdoc />
    public ValueTask ScrollToItemAsync(int index, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask RefreshAsync()
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    int? IDataViewPositions<TItem>.GetEdgePosition(bool last)
        => GetVisibleNodes() is { Count: > 0 } visible ? last ? visible.Count - 1 : 0 : null;

    /// <inheritdoc />
    ValueTask<IReadOnlyList<TItem>> IDataViewPositions<TItem>.ReadAsync(int from, int to, CancellationToken cancellationToken)
    {
        var visible = GetVisibleNodes();
        var start = Math.Clamp(from, 0, visible.Count);
        var stop = Math.Clamp(to + 1, start, visible.Count);

        return ValueTask.FromResult<IReadOnlyList<TItem>>(visible[start..stop]);
    }

    /// <inheritdoc />
    ValueTask IDataViewPositions<TItem>.ScrollToPositionAsync(int position, CancellationToken cancellationToken)
        => _virtualizer is { } virtualizer ? new ValueTask(virtualizer.ScrollToIndexAsync(position, cancellationToken: cancellationToken)) : ValueTask.CompletedTask;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _parameterRevision.Observe(parameters);

        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataTree<>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        _isCompanion = SelectsCurrent;
        _inputRegistration = new InputRegistration(InputRouter, HandleActionAsync);
        _dataViewRegistration = new DataViewRegistration(ViewKey, () => L["Tree"], Glyph.FolderTree, this) { IsCompanion = _isCompanion };
        Interaction.RegisterView(_dataViewRegistration);
        View.Subscribe(HandleViewChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _collectionName = CollectionName.From(AdditionalAttributes);

        // Whether the tree stands beside the other views is fixed when it registers.
        if (SelectsCurrent != _isCompanion)
        {
            throw new InvalidOperationException($"{nameof(SelectsCurrent)} cannot change once {nameof(FormaDataTree<TItem>)} has registered.");
        }
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
        => RendererInfo.IsInteractive ? Task.CompletedTask : LoadRootsAsync();

    // The profile is made again for a focus mode or a reading direction other than the one it was made for.
    private InputProfile GetInputProfile()
    {
        var isRightToLeft = Context.CultureState.IsRightToLeft;

        if (_inputProfile is null || _profileKeepsFocus != KeepsFocus || _profileIsRightToLeft != isRightToLeft)
        {
            _profileKeepsFocus = KeepsFocus;
            _profileIsRightToLeft = isRightToLeft;
            _inputProfile = CreateInputProfile(isRightToLeft);
        }

        return _inputProfile;
    }

    // The inline arrows and type-ahead are the tree's in either mode, the arrows bound by the reading direction; the vertical keys, Home, End, Enter and a press on a row are its own only where its rows carry the focus, and the selection's where a marker does.
    private InputProfile CreateInputProfile(bool isRightToLeft)
    {
        List<InputRoute> routes =
        [
            new InputRoute(OpenAction, new KeyBinding(isRightToLeft ? "ArrowLeft" : "ArrowRight")),
            new InputRoute(CloseAction, new KeyBinding(isRightToLeft ? "ArrowRight" : "ArrowLeft")),
            new InputRoute(FindAction, new TypingBinding()),
        ];

        if (KeepsFocus)
        {
            routes.Add(new InputRoute(NextAction, new KeyBinding("ArrowDown")));
            routes.Add(new InputRoute(PreviousAction, new KeyBinding("ArrowUp")));
            routes.Add(new InputRoute(FirstAction, new KeyBinding("Home")));
            routes.Add(new InputRoute(LastAction, new KeyBinding("End")));
            routes.Add(new InputRoute(ChooseAction, new KeyBinding("Enter"), new ClickBinding { Button = 0 }));
        }

        return new InputProfile { InputRoutes = routes };
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _focusWatching = Context.DomWatcher.GetElement(RootElement).Focus.WatchAsync(HandleFocusStateAsync, FocusOptions);
            _focusState = await _focusWatching;
        }
        else if (_focusState is { } focusState)
        {
            await focusState.SyncAsync(RootElement, FocusOptions);
        }

        // The keys act on the node holding the focus, so they are routed only once the tree knows which node that is; a render landing while
        // the watch is still being made leaves them to the first render's pass.
        if (_focusState is not null)
        {
            await _inputRegistration.SyncAsync(RootElement, GetInputProfile(), wanted: IsActive);
        }

        // The hand-off watches the tree while it shows, and lets go of a root the tree no longer draws.
        if (firstRender)
        {
            _recovery = await Context.DomEngine.GetElement(RootElement).Focus.AttachAsync(RecoveryOptions, IsActive);
        }

        if (_recovery is { } recovery)
        {
            await recovery.SyncAsync(RootElement, RecoveryOptions, IsActive);
        }

        if (_pendingFocusKey is { } pending && await TryFocusAsync(pending))
        {
            _pendingFocusKey = null;
        }

        if (_focusesSelf)
        {
            _focusesSelf = false;

            try
            {
                await RootElement.FocusAsync();
            }
            catch (JSException)
            {
                // The tree left the page before the focus reached it.
            }
        }

        await LoadRootsAsync();
    }

    // The roots are asked for once the tree shows, and again after the source or the sort left the data view with none; a static render
    // runs no step after it renders, so there they are asked for as the tree's parameters are set.
    private async Task LoadRootsAsync()
    {
        if (IsActive && View.HasHierarchy && !View.AreRootsLoaded && !View.IsLoadingRoots && !_areRootsRequested)
        {
            _areRootsRequested = true;
            await Interaction.LoadRootsAsync();
            _areRootsRequested = false;
        }
    }

    private IReadOnlyList<TItem> GetRoots()
        => Admit(View.Roots);

    private IReadOnlyList<TItem> GetChildren(TItem parent)
        => Admit(View.GetChildren(parent));

    private IReadOnlyList<TItem> Admit(IReadOnlyList<TItem> nodes)
        => NodeFilter is { } filter ? [.. nodes.Where(filter)] : nodes;

    // A node closed over the focus hands it on, so the toggle waits until the tree knows where the focus is.
    internal async Task ToggleNodeAsync(TItem node)
    {
        if (_focusWatching is { } watching)
        {
            await watching;
        }

        await (View.IsOpen(node) ? Interaction.CloseNodeAsync(node) : Interaction.OpenNodeAsync(node));
    }

    // Choosing a node, by a press on its row or Enter, makes it current where the tree drives the other views, and activates it otherwise; a row with a marker leaves both to the selection.
    private Task HandleNodeClickAsync(TItem node)
        => SelectsCurrent ? Interaction.MakeCurrentAsync(node) : Interaction.ActivateAsync(node);

    // The keys act on the node holding the focus, whether on its row or on its marker: the inline arrows open and close, or step into a node and back out; the vertical keys walk the visible nodes; Enter chooses; a letter jumps to the next node starting with it.
    private async Task HandleActionAsync(string action, InputInfo input)
    {
        if (_focusedNode is not { } node)
        {
            return;
        }

        switch (action)
        {
            case OpenAction when View.CanOpen(node) && !View.IsOpen(node):
                await Interaction.OpenNodeAsync(node);
                break;
            case OpenAction when GetChildren(node) is { Count: > 0 } children:
                await MoveToAsync(children[0]);
                break;
            case CloseAction when View.IsOpen(node):
                await Interaction.CloseNodeAsync(node);
                break;
            case CloseAction when View.TryGetParent(node, out var parent) && parent is not null:
                await MoveToAsync(parent);
                break;
            case NextAction:
                await StepAsync(node, 1);
                break;
            case PreviousAction:
                await StepAsync(node, -1);
                break;
            case FirstAction when GetFocusableNodes() is { Count: > 0 } focusable:
                await MoveToAsync(focusable[0]);
                break;
            case LastAction when GetFocusableNodes() is { Count: > 0 } focusable:
                await MoveToAsync(focusable[^1]);
                break;
            case ChooseAction:
                await HandleNodeClickAsync(node);
                break;
            case FindAction when input is InputKeyInfo { Key: var letter }:
                await JumpToAsync(node, letter);
                break;
            default:
                break;
        }
    }

    // Type-ahead: the next visible node after the focused one whose name starts with the letter and that can take the focus, wrapping round to the first.
    private async Task JumpToAsync(TItem from, string letter)
    {
        var visible = GetVisibleNodes();
        var start = visible.FindIndex(candidate => IsSame(candidate, from));

        for (var step = 1; step <= visible.Count; step++)
        {
            var candidate = visible[(start + step) % visible.Count];

            if (View.CanFocus(candidate) && Context.CultureState.Culture.CompareInfo.IsPrefix(View.ItemName(candidate), letter, CompareOptions.IgnoreCase))
            {
                await MoveToAsync(candidate);
                return;
            }
        }
    }

    // The nearest visible node that can take the focus a step away from this one, the ends holding.
    private Task StepAsync(TItem from, int step)
    {
        var visible = GetVisibleNodes();
        var index = visible.FindIndex(candidate => IsSame(candidate, from));

        for (var next = index + step; next >= 0 && next < visible.Count; next += step)
        {
            if (View.CanFocus(visible[next]))
            {
                return MoveToAsync(visible[next]);
            }
        }

        return Task.CompletedTask;
    }

    // Moving to a node selects it alone where a marker carries the focus, and makes it current in a sidebar; the focus lands on the marker or the row.
    private async Task MoveToAsync(TItem node)
    {
        // A key pressed before the browser reports the focus this move lands acts from here, as the reader meant it to; a move that
        // stays where the focus is lands nothing for the browser to report.
        if (_focusedNode is not { } from || !IsSame(from, node))
        {
            _unreportedMoves.Add(View.ItemKey(node));
        }

        _focusedNode = node;

        if (HasSelection)
        {
            await Interaction.SelectItemAsync(node);
        }
        else if (SelectsCurrent)
        {
            await Interaction.MakeCurrentAsync(node);
        }

        await FocusNodeAsync(node);
    }

    private bool IsSame(TItem node, TItem other)
        => Equals(View.ItemKey(node), View.ItemKey(other));

    private bool IsCurrent(TItem node)
        => View.CurrentItem is { } current && IsSame(current, node);

    // A plain tree's tab stop is the node last focused in it while that node shows, and a sidebar's the current node, the page the reader
    // is on; either falls back to the current node, then the first visible node.
    private void SettleTabStop()
    {
        if (!KeepsFocus || _visibleRows.Count == 0)
        {
            _tabStopKey = null;
            return;
        }

        var focusable = _visibleRows.FindAll(row => View.CanFocus(row.Node));

        if (focusable.Count == 0)
        {
            _tabStopKey = null;
            return;
        }

        if (!SelectsCurrent && _lastFocusedKey is { } last && focusable.Exists(row => Equals(GetRowKey(row), last)))
        {
            _tabStopKey = last;
            return;
        }

        var current = View.CurrentItem is { } item ? focusable.FindIndex(row => IsSame(row.Node, item)) : -1;

        _tabStopKey = GetRowKey(focusable[Math.Max(current, 0)]);
    }

    // The nodes this render draws, the focus handed on where the node holding it went away or can no longer take it.
    private void SettleRows()
    {
        var rows = GetVisibleRows();
        HashSet<object> remaining = [.. rows.Where(row => View.CanFocus(row.Node)).Select(GetRowKey)];

        HandFocusOn(rows, remaining);

        if (_pendingFocusKey is { } pending && !remaining.Contains(pending))
        {
            _pendingFocusKey = null;
        }

        // The node Tab returns to moves where the focus would have gone had it stayed on that node, which leaves the page's focus alone.
        if (_lastFocusedKey is { } last && !remaining.Contains(last))
        {
            var gone = _visibleRows.FindIndex(row => Equals(GetRowKey(row), last));
            _lastFocusedKey = gone >= 0 ? FindHeirKey(_visibleRows, gone, remaining) : null;
        }

        if (_heirKey is { } heir && !remaining.Contains(heir))
        {
            _heirKey = null;
        }

        _visibleRows = rows;
        SettleTabStop();
    }

    // When the node holding the focus goes away, the focus moves to the nearest node that remains: an ancestor closed over it, else the
    // next sibling, the previous sibling or the parent of the branch that went, else the tree itself. None of it changes the selection.
    // The render that takes the node away marks the one taking the focus, which the focus behavior focuses as the render lands.
    private void HandFocusOn(List<VisibleNode> rows, HashSet<object> remaining)
    {
        if (_focusedNode is not { } focused || remaining.Contains(View.ItemKey(focused)))
        {
            return;
        }

        var gone = _visibleRows.FindIndex(row => IsSame(row.Node, focused));

        if (gone < 0)
        {
            return;
        }

        _focusedNode = default;
        _unreportedMoves.Clear();

        if (FindHeirKey(_visibleRows, gone, remaining) is { } heir)
        {
            _pendingFocusKey = _heirKey = heir;
        }
        else if (rows.Count == 0)
        {
            _holdsFocus = true;
            _focusesSelf = true;
        }
    }

    // The key of the row taking the focus from the row at gone, found from the top of the branch that went, below the nearest of its
    // ancestors that remains.
    private object? FindHeirKey(List<VisibleNode> previous, int gone, HashSet<object> remaining)
    {
        var parent = GetParentRow(previous, gone);

        while (parent >= 0 && !remaining.Contains(GetRowKey(previous[parent])))
        {
            gone = parent;
            parent = GetParentRow(previous, gone);
        }

        return FindHeir(previous, gone, parent, remaining) is { } heir ? GetRowKey(previous[heir]) : null;
    }

    // The row taking the focus from the branch at this one: its parent where the parent closed over it, else a sibling that remains, the
    // next before the previous, else the parent.
    private int? FindHeir(List<VisibleNode> previous, int gone, int parent, HashSet<object> remaining)
    {
        if (parent >= 0 && !View.IsOpen(previous[parent].Node))
        {
            return parent;
        }

        var depth = previous[gone].Depth;

        for (var index = gone + 1; index < previous.Count && previous[index].Depth >= depth; index++)
        {
            if (previous[index].Depth == depth && remaining.Contains(GetRowKey(previous[index])))
            {
                return index;
            }
        }

        for (var index = gone - 1; index >= 0 && previous[index].Depth >= depth; index--)
        {
            if (previous[index].Depth == depth && remaining.Contains(GetRowKey(previous[index])))
            {
                return index;
            }
        }

        return parent >= 0 ? parent : null;
    }

    // A row's parent is the nearest row before it standing a level up.
    private static int GetParentRow(List<VisibleNode> rows, int index)
    {
        for (var candidate = index - 1; candidate >= 0; candidate--)
        {
            if (rows[candidate].Depth < rows[index].Depth)
            {
                return candidate;
            }
        }

        return -1;
    }

    private object GetRowKey(VisibleNode row)
        => View.ItemKey(row.Node);

    // The nodes the virtualizer keeps drawn wherever the tree scrolls: the one holding the focus, the one Tab returns to, a row's or a
    // marker's, and the one a move is bringing the focus to. The selection behavior moves among the markers drawn, so the nodes either
    // side of the marker last focused stay drawn too: an arrow's move takes the focus off one marker before it lands on the next, and
    // the next has to be drawn through the render between.
    private HashSet<object> GetPinnedKeys()
    {
        HashSet<object> keys = [];
        Pin(_focusedNode is { } focused ? View.ItemKey(focused) : null);
        Pin(HasSelection ? _lastFocusedKey : _tabStopKey);
        Pin(_pendingFocusKey);

        if (HasSelection && _lastFocusedKey is { } marked && _visibleRows.FindIndex(row => Equals(GetRowKey(row), marked)) is var index and >= 0)
        {
            Pin(index > 0 ? GetRowKey(_visibleRows[index - 1]) : null);
            Pin(index < _visibleRows.Count - 1 ? GetRowKey(_visibleRows[index + 1]) : null);
        }

        return keys;

        void Pin(object? key)
        {
            if (key is not null)
            {
                keys.Add(key);
            }
        }
    }

    // A marker keeps the tab stop once focused, which the selection behavior leaves on it.
    private void HandleNodeFocusIn(TItem node)
    {
        // A report of a move a later one has passed is out of date; the last move's, or a focus the reader moved, is where the focus is.
        var moved = _unreportedMoves.IndexOf(View.ItemKey(node));

        if (moved >= 0 && moved < _unreportedMoves.Count - 1)
        {
            _unreportedMoves.RemoveRange(0, moved + 1);
            return;
        }

        _unreportedMoves.Clear();
        _focusedNode = node;
        _lastFocusedKey = View.ItemKey(node);
        _holdsFocus = false;
        _heirKey = null;
    }

    // The mark on the element taking the focus from a node that went away: the node's marker where a marker carries the focus, else its row.
    internal string? GetHeirMark(object key, bool marker)
        => marker == HasSelection && Equals(_heirKey, key) ? string.Empty : null;

    internal string? GetNodeTabIndex(object key)
        => KeepsFocus ? Equals(_tabStopKey, key) ? "0" : "-1" : null;

    // A node is drawn by these, one bit each: those the other views share, then above them whether it is the current node, can open, is open, is
    // loading its children, is the tab stop or a row that is not, and takes the focus when the node holding it goes away, by its row or by its box.
    internal int GetNodeState(TItem node)
    {
        var key = View.ItemKey(node);
        var canOpen = View.CanOpen(node);

        var own = (IsCurrent(node) ? 1 : 0)
            | (canOpen ? 2 : 0)
            | (canOpen && View.IsOpen(node) ? 4 : 0)
            | (View.IsLoadingChildren(node) ? 8 : 0)
            | (KeepsFocus ? Equals(_tabStopKey, key) ? 16 : 32 : 0)
            | (GetHeirMark(key, marker: false) is not null ? 64 : 0)
            | (GetHeirMark(key, marker: true) is not null ? 128 : 0);

        return DataViewItemGate.GetState(View, node) | (own << DataViewItemGate.SharedBitCount);
    }

    // A node taking the focus draws the tree again, which pins the node it holds.
    private Task HandleFocusStateAsync(FocusState state)
        => InvokeAsync(() =>
        {
            if (state.IsWithin && state.Item is { } item && int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var token) && TryGetVisibleNode(token, out var node))
            {
                HandleNodeFocusIn(node);
            }
            else
            {
                HandleNodeFocusOut();
            }

            StateHasChanged();
        });

    private bool TryGetVisibleNode(int token, [MaybeNullWhen(false)] out TItem node)
    {
        foreach (var row in _visibleRows)
        {
            if (View.GetItemToken(row.Node) == token)
            {
                node = row.Node;
                return true;
            }
        }

        node = default;
        return false;
    }

    // A node losing the focus to a move the browser has yet to report leaves the focus where that move sent it.
    private void HandleNodeFocusOut()
    {
        if (_unreportedMoves.Count == 0)
        {
            _focusedNode = default;
        }
    }

    private List<TItem> GetVisibleNodes()
        => GetVisibleRows().ConvertAll(row => row.Node);

    private List<TItem> GetFocusableNodes()
        => GetVisibleNodes().FindAll(View.CanFocus);

    // The nodes a reader can see, from the roots down through every open node, each with its level and its place among its siblings.
    private List<VisibleNode> GetVisibleRows()
    {
        List<VisibleNode> visible = [];
        Collect(GetRoots(), 1);

        return visible;

        void Collect(IReadOnlyList<TItem> nodes, int depth)
        {
            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                visible.Add(new VisibleNode(node, depth, index + 1, nodes.Count, visible.Count));

                if (View.CanOpen(node) && View.IsOpen(node))
                {
                    Collect(GetChildren(node), depth + 1);
                }
            }
        }
    }

    // A node's element, or its marker's where a marker carries the focus; one scrolled out of a virtualizing tree has none to focus.
    private async Task<bool> TryFocusAsync(object key)
    {
        ElementReference? target = HasSelection
            ? _nodeMarkers.TryGetValue(key, out var marker) ? marker.RootElement : null
            : _nodeElements.TryGetValue(key, out var element) ? element : null;

        if (target is not { } focusable)
        {
            return false;
        }

        try
        {
            await focusable.FocusAsync();

            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }

    // A node a virtualizing tree has scrolled out of its rendered range is scrolled back first, and focused once it is in view.
    private async Task FocusNodeAsync(TItem node)
    {
        var key = View.ItemKey(node);
        _pendingFocusKey = null;

        if (await TryFocusAsync(key))
        {
            return;
        }

        var index = _visibleRows.FindIndex(row => IsSame(row.Node, node));

        // A move whose focus lands nowhere leaves nothing for the browser to report.
        if (!IsVirtualizing || _virtualizer is not { } virtualizer || index < 0)
        {
            if (_unreportedMoves.LastIndexOf(key) is var unreported and >= 0)
            {
                _unreportedMoves.RemoveAt(unreported);
            }

            return;
        }

        _pendingFocusKey = key;

        if (await virtualizer.ScrollToIndexAsync(index) && Equals(_pendingFocusKey, key) && await TryFocusAsync(key))
        {
            _pendingFocusKey = null;
        }
        else if (Equals(_pendingFocusKey, key))
        {
            // The node is drawn where it stands, pinned, and focused once that render is on the page.
            StateHasChanged();
        }
    }

    /// <summary>A node the tree shows: the node, its level, its place among its siblings, and its place among the visible nodes, where the tree virtualizes.</summary>
    private readonly record struct VisibleNode(TItem Node, int Depth, int? Position, int? SetSize, int? Row);

    internal string? GetNodeClass(TItem node)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-tree-node");
        builder.Add(View.ItemClass?.Invoke(node));
        builder.Add("forma-data-tree-node-current", IsCurrent(node));

        if (HasSelection)
        {
            builder.Add("forma-data-tree-node-selected", View.IsSelected(node));
            builder.Add("forma-data-tree-node-disabled", View.IsMuted(node));
        }

        return builder.ToStringOrNull();
    }

    // A node is named by its name alone, its disclosure's label left out of it.
    internal string GetNodeNameId(TItem node)
        => $"{ElementId}-node-{View.GetItemToken(node)}";

    private string GetNodeChildrenId(TItem node)
        => $"{ElementId}-children-{View.GetItemToken(node)}";

    internal static string GetNodeStyle(int depth)
        => string.Create(CultureInfo.InvariantCulture, $"--forma-data-tree-depth: {depth - 1}");

    // A tree with a selection tells each node's part in it; a sidebar is a single-select tree whose selected node is the current one.
    internal string? GetAriaSelected(TItem node)
        => HasSelection ? View.IsSelectable(node) ? View.IsSelected(node) ? "true" : "false" : null
            : SelectsCurrent ? IsCurrent(node) ? "true" : "false"
            : null;

    private Task HandleViewChangedAsync()
        => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-data-tree")
            .Add("forma-data-tree-dense", Dense)
            .Add("forma-data-tree-companion", _isCompanion && View.Layout == DataViewLayout.BuiltIn);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(_dataViewRegistration);

        if (_focusState is { } focusState)
        {
            await focusState.DisposeAsync();
        }

        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
