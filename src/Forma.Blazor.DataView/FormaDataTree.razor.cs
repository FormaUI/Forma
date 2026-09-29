namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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

    // The inline arrows are the tree's in either mode, bound by the reading direction; the vertical keys, Home, End and Enter are its own only where its rows carry the focus, and the selection's where a marker does.
    private InputProfile CreateInputProfile()
    {
        var rightToLeft = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        List<InputRoute> routes =
        [
            new InputRoute(OpenAction, new KeyBinding(rightToLeft ? "ArrowLeft" : "ArrowRight")),
            new InputRoute(CloseAction, new KeyBinding(rightToLeft ? "ArrowRight" : "ArrowLeft")),
        ];

        if (KeepsFocus)
        {
            routes.Add(new InputRoute(NextAction, new KeyBinding("ArrowDown")));
            routes.Add(new InputRoute(PreviousAction, new KeyBinding("ArrowUp")));
            routes.Add(new InputRoute(FirstAction, new KeyBinding("Home")));
            routes.Add(new InputRoute(LastAction, new KeyBinding("End")));
            routes.Add(new InputRoute(ChooseAction, new KeyBinding("Enter")));
        }

        return new InputProfile { InputRoutes = routes };
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _inputRegistration.SyncAsync(RootElement, _inputProfile, wanted: IsActive);

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

    // A press on the node's row makes it current where the tree drives the other views, and activates it otherwise; a row with a marker leaves the press to the selection.
    private Task HandleNodeClickAsync(TItem node)
        => SelectsCurrent ? Interaction.MakeCurrentAsync(node) : Interaction.ActivateAsync(node);

    // The keys act on the node holding the focus, whether on its row or on its marker: the inline arrows open and close, or step into a node and back out; the vertical keys walk the visible nodes; Enter chooses.
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
            default:
                break;
        }
    }

    // Type-ahead is the one key the router does not route: a letter jumps to the next node starting with it.
    private Task HandleNodeKeyDownAsync(KeyboardEventArgs args, TItem node)
        => args.Key.Length == 1 && !args.CtrlKey && !args.MetaKey && !args.AltKey && !char.IsWhiteSpace(args.Key[0])
            ? JumpToAsync(node, args.Key)
            : Task.CompletedTask;

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

            if (_nodeMarkers.TryGetValue(View.ItemKey(node), out var marker))
            {
                await marker.RootElement.FocusAsync();
            }

            return;
        }

        if (SelectsCurrent)
        {
            await Interaction.MakeCurrentAsync(node);
        }

        if (_nodeElements.TryGetValue(View.ItemKey(node), out var element))
        {
            await element.FocusAsync();
        }
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
    {
        List<TItem> visible = [];
        Collect(GetRoots());

        return visible;

        void Collect(IReadOnlyList<TItem> nodes)
        {
            foreach (var node in nodes)
            {
                visible.Add(node);

                if (View.CanOpen(node) && View.IsOpen(node))
                {
                    Collect(GetChildren(node));
                }
            }
        }
    }

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
