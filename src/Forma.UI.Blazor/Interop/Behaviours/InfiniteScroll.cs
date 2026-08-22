namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Asks for the next page as a sentinel element comes into view.</summary>
internal sealed class InfiniteScroll(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/InfiniteScroll.js")
{
    /// <summary>Attaches the sentinel, invoking <c>HandleReachedAsync</c> on <paramref name="owner"/> each time it comes into view.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference sentinel, DotNetObjectReference<TOwner> owner, ElementReference? root = null)
        where TOwner : class
        => AttachCoreAsync(sentinel, new { owner, root });
}
