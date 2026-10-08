namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>Moves a container to its center.</summary>
public sealed class Scroll(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/Scroll.js")
{
    /// <summary>Centers the container's scroll position over its content.</summary>
    public ValueTask<bool> ToCenterAsync(ElementReference container)
        => InvokeModuleAsync("toCenter", container);
}
