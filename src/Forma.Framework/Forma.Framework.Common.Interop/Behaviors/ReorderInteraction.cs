namespace Forma.Framework.Common.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Drags or keys an item into a new position in its list.</summary>
public sealed class ReorderInteraction(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/common/scripts/behaviors/ReorderInteraction.js")
{
    /// <summary>Attaches reordering to the list, invoking <c>HandleReorderAsync</c> on <paramref name="owner"/> once a move completes.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference list, DotNetObjectReference<TOwner> owner, bool dragFromItem)
        where TOwner : class
        => AttachCoreAsync(list, new { owner, dragFromItem });
}
