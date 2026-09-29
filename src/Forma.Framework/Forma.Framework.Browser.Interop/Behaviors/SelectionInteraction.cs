namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Click and keyboard selection across a list of keyed items, and Enter on an item's marker activating it.</summary>
public sealed class SelectionInteraction(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/SelectionInteraction.js")
{
    /// <summary>Attaches selection to the list, invoking <c>HandleSelectAsync</c>, <c>HandleRangeSelectAsync</c> or <c>HandleActivateAsync</c> on <paramref name="owner"/>, and where it seeks, <c>HandleSeekAsync</c> and <c>HandleRangeSelectByPositionAsync</c>.</summary>
    /// <param name="container">The element the items stand in.</param>
    /// <param name="owner">The owner told what was selected or activated.</param>
    /// <param name="itemSelector">The selector naming the item a marker sits in.</param>
    /// <param name="itemKeyAttribute">The attribute an item carries its token in, so an item without a marker can be activated; its marker's token when omitted.</param>
    /// <param name="controlSelector">The selector naming the parts of an item that hold its own controls, whose press activates nothing.</param>
    /// <param name="selects">Whether a press and the arrows select; otherwise a press activates, as it does in a list whose items are selected by their boxes alone.</param>
    /// <param name="positionAttribute">The attribute an item carries its position in the owner's order in, where the owner seeks.</param>
    /// <param name="seeks">Whether the owner holds more items than the page draws, so a move to the list's ends is asked of it and a range runs by position.</param>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference container, DotNetObjectReference<TOwner> owner, string itemSelector, string? itemKeyAttribute = null, string? controlSelector = null, bool selects = true, string? positionAttribute = null, bool seeks = false)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemSelector);

        return AttachCoreAsync(container, new { owner, itemSelector, itemKeyAttribute, controlSelector, selects, positionAttribute, seeks });
    }

    /// <summary>Lands the focus on the item at <paramref name="position"/> once it is drawn, answering the move <paramref name="request"/> named, unless the reader moved on meanwhile.</summary>
    /// <param name="request">The request the owner answers.</param>
    /// <param name="position">The position of the item the move reached.</param>
    public ValueTask<bool> FocusItemAsync(int request, int position)
        => InvokeAsync("focusItem", request, position);

    /// <summary>Lets go of a move asked of the owner, which it can no longer answer.</summary>
    public ValueTask<bool> CancelSeekAsync()
        => InvokeAsync("cancelSeek");
}
