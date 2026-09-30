namespace Forma.Blazor.Components.DataView.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Tells an owner how many columns a grid lays out, counted from its resolved tracks each time it is laid out again.</summary>
internal sealed class GridColumns(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.DataView/scripts/GridColumns.js")
{
    /// <summary>Attaches to <paramref name="target"/>; the <paramref name="owner"/> is told through <c>HandleGridColumnsAsync</c> how many columns it lays out whenever the count changes.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, new { owner });
    }
}
