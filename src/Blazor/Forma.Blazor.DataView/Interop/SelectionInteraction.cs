namespace Forma.Blazor.Components.DataView.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Click and keyboard selection across a list of keyed items, and Enter on an item's marker activating it.</summary>
internal sealed class SelectionInteraction(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.DataView/scripts/SelectionInteraction.js")
{
    /// <summary>Attaches selection to the list, invoking <c>HandleSelectAsync</c>, <c>HandleRangeSelectAsync</c> or <c>HandleActivateAsync</c> on <paramref name="owner"/>, and where it seeks, <c>HandleSeekAsync</c> and <c>HandleRangeSelectByPositionAsync</c>.</summary>
    /// <param name="container">The element the items stand in.</param>
    /// <param name="owner">The owner told what was selected or activated.</param>
    /// <param name="itemSelector">The selector naming the item a marker sits in.</param>
    /// <param name="itemKeyAttribute">The attribute an item carries its token in, where null reads the token from its marker.</param>
    /// <param name="controlSelector">The selector naming the parts of an item that hold its own controls, whose presses leave the item's selection and activation alone.</param>
    /// <param name="selects">Whether a press and the arrows select, where false makes a press activate.</param>
    /// <param name="moves">Whether the arrows, Home and End move the focus among the markers, which then stand as one tab stop.</param>
    /// <param name="positionAttribute">The attribute an item carries its position in the owner's order in.</param>
    /// <param name="seeks">Whether the owner holds more items than the page draws, so a move to the list's ends is asked of it and a range runs by position.</param>
    /// <param name="itemRootSelector">The selector naming the element each item is drawn as, whose controls are the item's.</param>
    /// <param name="actionsSelector">The selector naming the control within an item that opens its actions, which Shift+F10 and the Menu key on the item press.</param>
    /// <param name="gathersControls">Whether Tab reaches the active item's controls alone, every other item's standing out of the Tab sequence.</param>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference container, DotNetObjectReference<TOwner> owner, string itemSelector, string? itemKeyAttribute = null, string? controlSelector = null, bool selects = true, bool moves = true, string? positionAttribute = null, bool seeks = false, string? itemRootSelector = null, string? actionsSelector = null, bool gathersControls = false)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemSelector);

        return AttachCoreAsync(container, new AttachOptions<TOwner>
        {
            Owner = owner,
            ItemSelector = itemSelector,
            ItemKeyAttribute = itemKeyAttribute,
            ControlSelector = controlSelector,
            Selects = selects,
            Moves = moves,
            PositionAttribute = positionAttribute,
            Seeks = seeks,
            ItemRootSelector = itemRootSelector,
            ActionsSelector = actionsSelector,
            GathersControls = gathersControls,
        });
    }

    /// <summary>Lands the focus on the item at <paramref name="position"/> once it is drawn, answering the move <paramref name="request"/> named, unless the reader moved on meanwhile.</summary>
    /// <param name="request">The request the owner answers.</param>
    /// <param name="position">The position of the item the move reached.</param>
    public ValueTask<bool> FocusItemAsync(int request, int position)
        => InvokeAsync("focusItem", request, position);

    /// <summary>Lets go of a move asked of the owner, which it can no longer answer.</summary>
    public ValueTask<bool> CancelSeekAsync()
        => InvokeAsync("cancelSeek");

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required string ItemSelector { get; init; }

        public required string? ItemKeyAttribute { get; init; }

        public required string? ControlSelector { get; init; }

        public required bool Selects { get; init; }

        public required bool Moves { get; init; }

        public required string? PositionAttribute { get; init; }

        public required bool Seeks { get; init; }

        public required string? ItemRootSelector { get; init; }

        public required string? ActionsSelector { get; init; }

        public required bool GathersControls { get; init; }
    }
}
