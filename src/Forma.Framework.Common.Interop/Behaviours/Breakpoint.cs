namespace Forma.Framework.Common.Interop.Behaviours;

using Forma.Framework.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Tells an owner which layout tier a stylesheet has put the target in, read from a custom property each time the target is laid out again.</summary>
public sealed class Breakpoint(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Common.Interop/scripts/behaviours/Breakpoint.js")
{
    /// <summary>Attaches to <paramref name="target"/>; the <paramref name="owner"/> is told through <c>HandleBreakpointAsync</c> the value of <paramref name="property"/> whenever it changes.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, string property, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, new { owner, property });
    }
}
