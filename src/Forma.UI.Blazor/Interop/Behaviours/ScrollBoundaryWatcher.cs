namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Reports how much of a scrolling track is still out of view.</summary>
internal sealed class ScrollBoundaryWatcher(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/ScrollBoundaryWatcher.js")
{
    /// <summary>Attaches the watcher, invoking <c>HandleOverflowAsync</c> on <paramref name="owner"/> whenever the overflow changes.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference track, DotNetObjectReference<TOwner> owner)
        where TOwner : class
        => AttachCoreAsync(track, new { owner });
}
