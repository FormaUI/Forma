namespace Forma.Framework.Common.Interop.Behaviours;

using Forma.Framework.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Click and keyboard selection across a list of keyed items.</summary>
public sealed class SelectionInteraction(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Common.Interop/scripts/behaviours/SelectionInteraction.js")
{
    /// <summary>Attaches selection to the list, invoking <c>HandleSelectAsync</c> or <c>HandleRangeSelectAsync</c> on <paramref name="owner"/>.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference container, DotNetObjectReference<TOwner> owner)
        where TOwner : class
        => AttachCoreAsync(container, new { owner });
}
