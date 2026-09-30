namespace Forma.Blazor.Components.Explorer.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Tells an owner which layout tier a stylesheet has put the target in, read from a custom property each time the target is laid out again.</summary>
internal sealed class Breakpoint(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.Explorer/scripts/Breakpoint.js")
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
