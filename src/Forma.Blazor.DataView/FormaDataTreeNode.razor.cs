namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>One node of a tree: its disclosure, its box where selection is on, its thumbnail, its name and the caller's content.</summary>
/// <typeparam name="TItem">The item the node shows.</typeparam>
/// <remarks>The node draws again only when its item, its place, what the tree draws every node by or its own state changed.</remarks>
public sealed partial class FormaDataTreeNode<TItem> : FormaComponentBase
    where TItem : notnull
{
    private DataViewItemGate _gate;

    /// <summary>The tree the node is drawn for.</summary>
    [Parameter, EditorRequired] public FormaDataTree<TItem> Tree { get; set; } = default!;

    /// <summary>The item the node shows.</summary>
    [Parameter, EditorRequired] public TItem Item { get; set; } = default!;

    /// <summary>The node's level, the roots at one.</summary>
    [Parameter] public int Depth { get; set; }

    /// <summary>The node's place among its siblings, where the tree draws them.</summary>
    [Parameter] public int? SiblingPosition { get; set; }

    /// <summary>How many siblings the node has, itself among them, where the tree draws them.</summary>
    [Parameter] public int? SetSize { get; set; }

    /// <summary>The node's place among the nodes shown, where the tree virtualizes.</summary>
    [Parameter] public int? Position { get; set; }

    /// <summary>The identifier of the group of children the node owns, while it shows them.</summary>
    [Parameter] public string? OwnsId { get; set; }

    /// <summary>The token the selection behavior names the node by, which the tree asks for each time it draws.</summary>
    [Parameter] public int Token { get; set; }

    /// <summary>What the tree draws every node by.</summary>
    [Parameter] public int ViewRevision { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    // The field a node's box names to the input base as its value expression.
    private bool NodeSelection { get; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataTree<TItem>);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Item);

        if (Tree is null || View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataTreeNode<TItem>)} must be rendered inside a {nameof(FormaDataTree<TItem>)}.");
        }

        // The first draw is not asked about, so what it draws by is recorded here.
        if (!_gate.IsRecorded)
        {
            _gate.Record(Item, Position, ViewRevision, CultureState.Revision, Tree.GetNodeState(Item), GetShape());
        }
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var state = Tree.GetNodeState(Item);
        var shape = GetShape();

        if (!_gate.Differs(Item, Position, ViewRevision, CultureState.Revision, state, shape))
        {
            return false;
        }

        _gate.Record(Item, Position, ViewRevision, CultureState.Revision, state, shape);

        return true;
    }

    // Where the node stands: its level, its place among its siblings and the group of children it owns.
    private (int Depth, int? SiblingPosition, int? SetSize, string? OwnsId) GetShape()
        => (Depth, SiblingPosition, SetSize, OwnsId);
}
