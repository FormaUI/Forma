namespace Forma.Blazor.Components.DataView.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Tells an owner how many columns a grid lays out.</summary>
/// <remarks>The count is read from the grid's resolved tracks each time the grid is laid out again.</remarks>
internal sealed class GridColumns(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.DataView/scripts/GridColumns.js")
{
    /// <summary>Attaches to <paramref name="target"/>; the <paramref name="owner"/> is told through <c>HandleGridColumnsAsync</c> how many columns it lays out whenever the count changes.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, new AttachOptions<TOwner> { Owner = owner });
    }

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }
    }
}
