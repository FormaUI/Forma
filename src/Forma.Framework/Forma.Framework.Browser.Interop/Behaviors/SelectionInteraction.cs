namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Click and keyboard selection across a list of keyed items, and Enter on an item's marker activating it.</summary>
public sealed class SelectionInteraction(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/SelectionInteraction.js")
{
    /// <summary>Attaches selection to the list, invoking <c>HandleSelectAsync</c>, <c>HandleRangeSelectAsync</c> or <c>HandleActivateAsync</c> on <paramref name="owner"/>.</summary>
    /// <param name="container">The element the items stand in.</param>
    /// <param name="owner">The owner told what was selected or activated.</param>
    /// <param name="itemSelector">The selector naming the item a marker sits in.</param>
    /// <param name="selects">Whether a press and the arrows select; a list whose items are selected by their boxes alone takes activation only.</param>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference container, DotNetObjectReference<TOwner> owner, string itemSelector, bool selects = true)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemSelector);

        return AttachCoreAsync(container, new { owner, itemSelector, selects });
    }
}
