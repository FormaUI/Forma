namespace Forma.Framework.Browser.Interop.Behaviors;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Drags or keys an item into a new position in its list.</summary>
public sealed class ReorderInteraction(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/ReorderInteraction.js")
{
    /// <summary>Attaches reordering to the list, invoking <c>HandleReorderAsync</c> on <paramref name="owner"/> once a move completes.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference list, DotNetObjectReference<TOwner> owner, bool dragFromItem)
        where TOwner : class
        => AttachCoreAsync(list, new AttachOptions<TOwner> { Owner = owner, DragFromItem = dragFromItem });

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required bool DragFromItem { get; init; }
    }
}
