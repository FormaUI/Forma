namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Drags or keys an item into a new position in its list.</summary>
internal sealed class ReorderInteraction(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/ReorderInteraction.js")
{
    /// <summary>Attaches reordering to the list, invoking <c>HandleReorderAsync</c> on <paramref name="owner"/> once a move completes.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference list, DotNetObjectReference<TOwner> owner, bool dragFromItem)
        where TOwner : class
        => AttachCoreAsync(list, new { owner, dragFromItem });
}
