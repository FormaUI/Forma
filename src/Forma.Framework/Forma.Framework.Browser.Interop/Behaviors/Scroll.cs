namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>What a surface does with scrolling: telling its owner when a sentinel comes into view, and moving a container to its center.</summary>
public sealed class Scroll(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/Scroll.js")
{
    /// <summary>Attaches the behaviors to <paramref name="target"/>.</summary>
    /// <remarks>The <paramref name="owner"/> is told through <c>HandleReachedAsync</c> when the target comes into view.</remarks>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, ScrollBehaviors behaviors, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, new
        {
            sentinel = behaviors.Sentinel is { } sentinel ? new { owner, root = sentinel.Root } : null,
        });
    }

    /// <summary>Centers the container's scroll position over its content.</summary>
    public ValueTask<bool> ToCenterAsync(ElementReference container)
        => InvokeModuleAsync("toCenter", container);
}
