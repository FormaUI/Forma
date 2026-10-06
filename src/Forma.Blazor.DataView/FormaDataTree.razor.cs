namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Blazor.Components.Inputs;
using Forma.Blazor.Components.Virtualization;
using Forma.Framework.Browser.Interop.Behaviors;
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

    private readonly Dictionary<object, ElementReference> _nodeElements = [];
    private readonly Dictionary<object, FormaCheckBox> _nodeMarkers = [];
    private readonly ParameterRevision _parameterRevision = new(_parametersLeftOut);
    private InputRegistration _inputRegistration = default!;
    private InputProfile _inputProfile = default!;
    private bool _profileKeepsFocus;
    private TItem? _focusedNode;
    private bool _isCompanion;
    private bool _areRootsRequested;
    private bool _isRevealing;
    private object? _tabStopKey;
    private object? _currentKey;
    private object? _pendingFocusKey;
    private object? _lastFocusedKey;
    private bool _holdsFocus;
    private bool _focusesSelf;
    private object? _heirKey;
    private Focus _focus = default!;
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
        => View.Virtualize && !View.EnablePagination;

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
        _focus = new Focus(Context.JS);
        Interaction.RegisterView(new DataViewRegistration(ViewKey, L["Tree"], Glyph.FolderTree, this) { IsCompanion = _isCompanion });
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

        if (_inputProfile is null || _profileKeepsFocus != KeepsFocus)
        {
            _profileKeepsFocus = KeepsFocus;
            _inputProfile = CreateInputProfile();
        }
    }

    // The inline arrows and type-ahead are the tree's in either mode, the arrows bound by the reading direction; the vertical keys, Home, End, Enter and a press on a row are its own only where its rows carry the focus, and the selection's where a marker does.
    private InputProfile CreateInputProfile()
    {
        var rightToLeft = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        List<InputRoute> routes =
        [
            new InputRoute(OpenAction, new KeyBinding(rightToLeft ? "ArrowLeft" : "ArrowRight")),
            new InputRoute(CloseAction, new KeyBinding(rightToLeft ? "ArrowRight" : "ArrowLeft")),
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
        await _inputRegistration.SyncAsync(RootElement, _inputProfile, wanted: IsActive);

        // The hand-off watches the tree while it shows, and lets go of a root the tree no longer draws.
        if (IsActive && !_focus.IsAttached)
        {
            await _focus.AttachAsync(RootElement, heir: HeirAttribute);
        }
        else if (!IsActive && _focus.IsAttached)
        {
            await _focus.DetachAsync();
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

        // The roots are asked for once the tree shows, and again after the source or the sort left the data view with none.
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

    internal Task ToggleNodeAsync(TItem node)
        => View.IsOpen(node) ? Interaction.CloseNodeAsync(node) : Interaction.OpenNodeAsync(node);

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
            case FirstAction:
                await MoveToAsync(GetVisibleNodes()[0]);
                break;
            case LastAction:
                await MoveToAsync(GetVisibleNodes()[^1]);
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

    // Type-ahead: the next visible node after the focused one whose name starts with the letter, wrapping round to the first.
    private async Task JumpToAsync(TItem from, string letter)
    {
        var visible = GetVisibleNodes();
        var start = visible.FindIndex(candidate => IsSame(candidate, from));

        for (var step = 1; step <= visible.Count; step++)
        {
            var candidate = visible[(start + step) % visible.Count];

            if (View.ItemName(candidate).StartsWith(letter, StringComparison.CurrentCultureIgnoreCase))
            {
                await MoveToAsync(candidate);
                return;
            }
        }
    }

    // The visible node a step away from this one, the ends holding.
    private Task StepAsync(TItem from, int step)
    {
        var visible = GetVisibleNodes();
        var index = visible.FindIndex(candidate => IsSame(candidate, from));

        return MoveToAsync(visible[Math.Clamp(index + step, 0, visible.Count - 1)]);
    }

    // Moving to a node selects it alone where a marker carries the focus, and makes it current in a sidebar; the focus lands on the marker or the row.
    private async Task MoveToAsync(TItem node)
    {
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

    // The current node is the tab stop while it shows; the first visible node otherwise.
    private void SettleTabStop()
    {
        if (!KeepsFocus)
        {
            _tabStopKey = null;
            return;
        }

        var current = View.CurrentItem is { } item ? _visibleRows.FindIndex(row => IsSame(row.Node, item)) : -1;

        _tabStopKey = _visibleRows.Count == 0 ? null : GetRowKey(_visibleRows[Math.Max(current, 0)]);
    }

    // The nodes this render draws, the focus handed on where the node holding it went away.
    private void SettleRows()
    {
        var rows = GetVisibleRows();
        HashSet<object> remaining = [.. rows.Select(GetRowKey)];

        HandFocusOn(rows, remaining);

        if (_pendingFocusKey is { } pending && !remaining.Contains(pending))
        {
            _pendingFocusKey = null;
        }

        if (_lastFocusedKey is { } last && !remaining.Contains(last))
        {
            _lastFocusedKey = null;
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

        var previous = _visibleRows;
        var gone = previous.FindIndex(row => IsSame(row.Node, focused));

        if (gone < 0)
        {
            return;
        }

        // The top of the branch that went, and the nearest of its ancestors that remains.
        var parent = GetParentRow(previous, gone);

        while (parent >= 0 && !remaining.Contains(GetRowKey(previous[parent])))
        {
            gone = parent;
            parent = GetParentRow(previous, gone);
        }

        _focusedNode = default;

        if (FindHeir(previous, gone, parent, remaining) is { } heir)
        {
            _pendingFocusKey = _heirKey = GetRowKey(previous[heir]);
        }
        else if (rows.Count == 0)
        {
            _holdsFocus = true;
            _focusesSelf = true;
        }
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

    // A node is drawn by these, one bit each: the two the other views share, then whether it is the current node, can open, is open, is
    // loading its children, is the tab stop or a row that is not, and takes the focus when the node holding it goes away, by its row or by its box.
    internal int GetNodeState(TItem node)
    {
        var key = View.ItemKey(node);
        var canOpen = View.CanOpen(node);

        return DataViewItemGate.GetState(View, node)
            | (IsCurrent(node) ? 4 : 0)
            | (canOpen ? 8 : 0)
            | (canOpen && View.IsOpen(node) ? 16 : 0)
            | (View.IsLoadingChildren(node) ? 32 : 0)
            | (KeepsFocus ? Equals(_tabStopKey, key) ? 64 : 128 : 0)
            | (GetHeirMark(key, marker: false) is not null ? 256 : 0)
            | (GetHeirMark(key, marker: true) is not null ? 512 : 0);
    }

    // The tree is the receiver, so a node taking the focus draws the tree again, which pins the node it holds.
    internal EventCallback<FocusEventArgs> GetNodeFocusInHandler(TItem node)
        => EventCallback.Factory.Create<FocusEventArgs>(this, () => HandleNodeFocusIn(node));

    internal EventCallback<FocusEventArgs> GetNodeFocusOutHandler()
        => EventCallback.Factory.Create<FocusEventArgs>(this, () => _focusedNode = default);

    private List<TItem> GetVisibleNodes()
        => GetVisibleRows().ConvertAll(row => row.Node);

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

        if (await TryFocusAsync(key) || !IsVirtualizing || _virtualizer is not { } virtualizer)
        {
            return;
        }

        var index = _visibleRows.FindIndex(row => IsSame(row.Node, node));

        if (index < 0)
        {
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
            builder.Add("forma-data-tree-node-disabled", !View.CanInteractWith(node));
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
        => HasSelection ? View.IsSelected(node) ? "true" : "false"
            : SelectsCurrent ? IsCurrent(node) ? "true" : "false"
            : null;

    private async Task HandleViewChangedAsync()
    {
        await InvokeAsync(StateHasChanged);

        // The reveal follows a change of the current node alone, so a node closed over the current one stays closed.
        var currentKey = View.CurrentItem is { } current ? View.ItemKey(current) : null;
        var hasChanged = !Equals(currentKey, _currentKey);
        _currentKey = currentKey;

        if (SelectsCurrent && hasChanged && !_isRevealing && View.CurrentItem is { } node)
        {
            await RevealAsync(node);
        }
    }

    // A sidebar shows a node that became current: the ancestors of one it does not show are opened, from the root down, as far as the data view knows them.
    private async Task RevealAsync(TItem node)
    {
        if (GetVisibleNodes().Any(candidate => IsSame(candidate, node)))
        {
            return;
        }

        List<TItem> ancestors = [];
        var step = node;

        while (View.TryGetParent(step, out var parent) && parent is not null)
        {
            ancestors.Add(parent);
            step = parent;
        }

        // Only a chain that reaches a root, one the data view has seen whole, opens.
        if (ancestors.Count == 0 || !View.Roots.Any(root => IsSame(root, step)))
        {
            return;
        }

        _isRevealing = true;

        try
        {
            for (var index = ancestors.Count - 1; index >= 0; index--)
            {
                if (!View.IsOpen(ancestors[index]))
                {
                    await Interaction.OpenNodeAsync(ancestors[index]);
                }
            }
        }
        finally
        {
            _isRevealing = false;
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-data-tree")
            .Add("forma-data-tree-dense", Dense)
            .Add("forma-data-tree-companion", _isCompanion);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(ViewKey);
        await _inputRegistration.DisposeAsync();
        await _focus.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
