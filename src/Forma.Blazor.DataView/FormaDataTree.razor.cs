namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Click;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

/// <summary>A view over a data view drawing the hierarchy as nodes under their parents, each opened, closed and selected from the keyboard, with a busy state while its children load.</summary>
/// <typeparam name="TItem">The item a node shows.</typeparam>
public sealed partial class FormaDataTree<TItem> : FormaElementBase, IDataViewViewport
    where TItem : notnull
{
    private const string ViewKey = "tree";
    private const string NextAction = "tree.next";
    private const string PreviousAction = "tree.previous";
    private const string FirstAction = "tree.first";
    private const string LastAction = "tree.last";
    private const string OpenAction = "tree.open";
    private const string CloseAction = "tree.close";
    private const string ChooseAction = "tree.choose";
    private const string FindAction = "tree.find";

    /// <summary>What a node shows beside its name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The item's depiction drawn before its name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ThumbnailTemplate { get; set; }

    /// <summary>Tightens the rows for names alone.</summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>Makes a chosen node the current node, whose children the other views show; the tree then stands beside them as a sidebar, out of the view switch, its choice the current node rather than the data view's selection.</summary>
    [Parameter] public bool SelectsCurrent { get; set; }

    /// <summary>Admits the items the tree shows, the folders alone for a sidebar over a source that holds files too; every item when omitted.</summary>
    [Parameter] public Func<TItem, bool>? NodeFilter { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    private CollectionName _collectionName = CollectionName.None;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private readonly Dictionary<object, ElementReference> _nodeElements = [];
    private readonly Dictionary<object, FormaCheckBox> _nodeMarkers = [];
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
    private Virtualize<VisibleNode>? _virtualizer;
    private List<VisibleNode> _visibleRows = [];

    // The field a node's box names to the input base; the selection is not a form field and nothing binds it.
    private bool NodeSelection { get; }

    private bool IsActive
        => _isCompanion || View.ActiveViewKey == ViewKey;

    // A sidebar's choice is the current node, so its rows carry no marker of the data view's selection.
    private bool HasSelection
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
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataTree<>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        _isCompanion = SelectsCurrent;
        _inputRegistration = new InputRegistration(InputRouter, HandleActionAsync);
        Interaction.RegisterView(new DataViewRegistration(ViewKey, L["Tree"], Glyph.FolderTree, this) { IsCompanion = _isCompanion });
        View.Subscribe(HandleViewChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _collectionName = CollectionName.From(AdditionalAttributes);

        // Whether the tree stands beside the other views is decided as it registers; it does not change under a mounted tree.
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

        if (_pendingFocusKey is { } pending && await TryFocusAsync(pending))
        {
            _pendingFocusKey = null;
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

    private Task ToggleNodeAsync(TItem node)
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

        var visible = GetVisibleNodes();
        var current = View.CurrentItem is { } item ? visible.Find(candidate => IsSame(candidate, item)) : default;

        _tabStopKey = visible.Count == 0 ? null : View.ItemKey(current ?? visible[0]);
    }

    private string? GetNodeTabIndex(object key)
        => KeepsFocus ? Equals(_tabStopKey, key) ? "0" : "-1" : null;

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
                visible.Add(new VisibleNode(node, depth, index + 1, nodes.Count));

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

    // A node a virtualizing tree has scrolled out of its window is scrolled back first, and focused once its row is drawn.
    private async Task FocusNodeAsync(TItem node)
    {
        var key = View.ItemKey(node);
        _pendingFocusKey = null;

        if (await TryFocusAsync(key) || !IsVirtualizing || _virtualizer is not { } virtualizer)
        {
            return;
        }

        var index = GetVisibleNodes().FindIndex(candidate => IsSame(candidate, node));

        if (index >= 0)
        {
            _pendingFocusKey = key;
            await virtualizer.ScrollToItemAsync(index);
            StateHasChanged();
        }
    }

    /// <summary>A node the tree shows: the node, its level, and its place among its siblings where the tree virtualizes.</summary>
    private readonly record struct VisibleNode(TItem Node, int Depth, int? Position, int? SetSize);

    private string? GetNodeClass(TItem node)
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
    private string GetNodeNameId(TItem node)
        => $"{ElementId}-node-{View.GetItemToken(node)}";

    private string GetNodeChildrenId(TItem node)
        => $"{ElementId}-children-{View.GetItemToken(node)}";

    private static string GetNodeStyle(int depth)
        => string.Create(CultureInfo.InvariantCulture, $"--forma-data-tree-depth: {depth - 1}");

    // A tree with a selection tells each node's part in it; a sidebar is a single-select tree whose selected node is the current one.
    private string? GetAriaSelected(TItem node)
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

        // A chain that does not reach a root is one the data view has not seen whole; nothing opens for it.
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
        await base.DisposeAsyncCore();
    }
}
