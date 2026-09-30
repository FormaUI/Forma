namespace Forma.Blazor.Components.Collections;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A list whose items can be reordered by dragging a handle or moving a focused item with the keyboard.</summary>
public sealed partial class FormaSortableList<TItem> : FormaElementBase
    where TItem : notnull
{
    /// <summary>The items the list renders, in their current order.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = default!;

    /// <summary>Raised with the reordered list after a drag or keyboard move.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> ItemsChanged { get; set; }

    /// <summary>The template of an item's content, rendered beside its drag handle.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

    /// <summary>The key that identifies an item across reorders.</summary>
    [Parameter] public Func<TItem, object> ItemKey { get; set; } = item => item;

    /// <summary>Whether reordering is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Where on a row a drag may start.</summary>
    [Parameter] public SortableDragFrom DragFrom { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private readonly Dictionary<object, FormaButton> _handles = [];
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

        foreach (var key in _handles.Keys.Except(Items.Select(ItemKey)).ToList())
        {
            _handles.Remove(key);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Disabled || (_isAttached && _attachedDragFrom == DragFrom))
        {
            return;
        }

        // DragFrom is fixed when the listeners bind, so a change of DragFrom binds them again.
        // Claimed before the await, so a render arriving mid-call attaches no second listener.
        _isAttached = true;
        _attachedDragFrom = DragFrom;
        _selfReference ??= DotNetObjectReference.Create(this);

        // The host's policy reaches the router before the keys the script declares as routes.
        await InputRouter.ConfigureAsync();
        _isAttached = await _reorderInteraction.AttachAsync(RootElement, _selfReference, DragFrom == SortableDragFrom.Item);
    }

    /// <summary>Moves focus to the first item's handle.</summary>
    public ValueTask FocusAsync()
        => !Disabled && Items.Count > 0 && _handles.TryGetValue(ItemKey(Items[0]), out var handle)
            ? handle.RootElement.FocusAsync()
            : ValueTask.CompletedTask;

    private string HandleLabel(int index)
    {
        var position = index + 1;
        var total = Items.Count;

        return L["Reorder item {Position} of {Total}. Drag, or use arrow keys to move.", position, total];
    }

    // Both indices are positions before the move; removing the source shifts a later target down by one.
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

    /// <summary>Moves the item at <paramref name="sourceIndex"/> before or after the item at <paramref name="targetIndex"/>.</summary>
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

        // An alternating zero-width space makes a repeated announcement a new string, which the live region reads again.
        _padsAnnouncement = !_padsAnnouncement;

        var position = newIndex + 1;
        var total = reordered.Count;

        _announcement = L["Moved to position {Position} of {Total}.", position, total]
            + (_padsAnnouncement ? "\u200B" : string.Empty);

        // The announcement renders before the host feeds the reordered list back.
        StateHasChanged();

        return ItemsChanged.InvokeAsync(reordered);
    }

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
