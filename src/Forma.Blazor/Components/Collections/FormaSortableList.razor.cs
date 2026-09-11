namespace Forma.Blazor.Components.Collections;

using Forma.Framework.Common.Interop.Behaviours;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A list whose items can be reordered by dragging a handle or moving a focused item with the keyboard.</summary>
public sealed partial class FormaSortableList<TItem> : FormaElementBase
    where TItem : notnull
{
    /// <summary>The items to display, in their current order.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = default!;

    /// <summary>Raised with the reordered list after a drag or keyboard move.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> ItemsChanged { get; set; }

    /// <summary>Renders the content of a single item, beside its drag handle.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

    /// <summary>Identifies an item across reorders, so its element is moved rather than recreated.</summary>
    [Parameter] public Func<TItem, object> ItemKey { get; set; } = item => item;

    /// <summary>Blocks reordering, showing the items in a fixed order.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Where on a row a drag may start.</summary>
    [Parameter] public SortableDragFrom DragFrom { get; set; }

    private ReorderInteraction _reorderInteraction = default!;
    private DotNetObjectReference<FormaSortableList<TItem>>? _selfReference;
    private bool _isAttached;
    private SortableDragFrom _attachedDragFrom;
    private string? _announcement;
    private bool _padsAnnouncement;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _reorderInteraction = new ReorderInteraction(Context.JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);
        ArgumentNullException.ThrowIfNull(ItemTemplate);
        ArgumentNullException.ThrowIfNull(ItemKey);
    }

    // A disabled list renders unfocusable, non-draggable handles, so there is nothing
    // for the script to listen for; skipped rather than attached and left idle.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Disabled || (_isAttached && _attachedDragFrom == DragFrom))
        {
            return;
        }

        // Where a drag may start is decided when the listeners are bound, so a change of
        // mind has to rebind rather than leave the script answering for the old one.
        // Claimed before the await so a render arriving mid-call cannot re-enter and
        // attach a second listener to the same list.
        _isAttached = true;
        _attachedDragFrom = DragFrom;
        _selfReference ??= DotNetObjectReference.Create(this);
        _isAttached = await _reorderInteraction.AttachAsync(RootElement, _selfReference, DragFrom == SortableDragFrom.Item);
    }

    private string HandleLabel(int index)
    {
        var position = index + 1;
        var total = Items.Count;

        return L["Reorder item {Position} of {Total}. Drag, or use arrow keys to move.", position, total];
    }

    // sourceIndex and targetIndex are the two items' positions before either moves; removing
    // the source shifts every later index down by one, which only matters when the target
    // was originally the later of the two.
    private static (List<TItem> Items, int NewIndex) Reorder(IReadOnlyList<TItem> items, int sourceIndex, int targetIndex, bool insertAfter)
    {
        var list = new List<TItem>(items);
        var item = list[sourceIndex];
        list.RemoveAt(sourceIndex);

        var shiftedTargetIndex = targetIndex > sourceIndex ? targetIndex - 1 : targetIndex;
        var insertIndex = Math.Clamp(shiftedTargetIndex + (insertAfter ? 1 : 0), 0, list.Count);

        list.Insert(insertIndex, item);
        return (list, insertIndex);
    }

    /// <summary>Moves the item at <paramref name="sourceIndex"/> next to the item at <paramref name="targetIndex"/>, invoked from the drag or keyboard script.</summary>
    [JSInvokable]
    public Task HandleReorderAsync(int sourceIndex, int targetIndex, bool insertAfter)
    {
        if (Disabled || sourceIndex == targetIndex
            || sourceIndex < 0 || sourceIndex >= Items.Count
            || targetIndex < 0 || targetIndex >= Items.Count)
        {
            return Task.CompletedTask;
        }

        var (reordered, newIndex) = Reorder(Items, sourceIndex, targetIndex, insertAfter);

        // A live region is skipped when its text is unchanged, so two moves landing on the
        // same position would announce once; an alternating zero-width space keeps each
        // announcement a distinct string without altering what is read out.
        _padsAnnouncement = !_padsAnnouncement;

        var position = newIndex + 1;
        var total = reordered.Count;

        _announcement = L["Moved to position {Position} of {Total}.", position, total]
            + (_padsAnnouncement ? "\u200B" : string.Empty);

        // Announced regardless of whether or when the host feeds the reordered list back,
        // since that round trip is the caller's to schedule, not this component's to wait on.
        StateHasChanged();

        return ItemsChanged.InvokeAsync(reordered);
    }

    // The row-drag affordances are withheld while disabled, so a row that cannot be dragged
    // does not offer a grab cursor or refuse to let its text be selected.
    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-sortable-list")
            .Add("forma-sortable-list-row-drag", DragFrom == SortableDragFrom.Item && !Disabled);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _reorderInteraction.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
