namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Drags a scroll container's contents with the pointer, the press a route on the input router.</summary>
public sealed class PanGesture(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/PanGesture.js")
{
    /// <summary>Attaches panning to the container.</summary>
    /// <param name="container">The scroll container.</param>
    /// <param name="action">The action the press is routed as.</param>
    /// <param name="whileClass">The class that marks the container as pannable.</param>
    public ValueTask<bool> AttachAsync(ElementReference container, string action, string whileClass)
        => AttachCoreAsync(container, new { action, whileClass });
}
