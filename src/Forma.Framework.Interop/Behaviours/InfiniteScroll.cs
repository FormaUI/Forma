namespace Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Asks for the next page as a sentinel element comes into view.</summary>
public sealed class InfiniteScroll(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Interop/scripts/behaviours/InfiniteScroll.js")
{
    /// <summary>Attaches the sentinel, invoking <c>HandleReachedAsync</c> on <paramref name="owner"/> each time it comes into view.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference sentinel, DotNetObjectReference<TOwner> owner, ElementReference? root = null)
        where TOwner : class
        => AttachCoreAsync(sentinel, new { owner, root });
}
