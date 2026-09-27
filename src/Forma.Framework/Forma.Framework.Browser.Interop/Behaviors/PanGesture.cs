namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Drags a scroll container's contents with the pointer while a class marks the container eligible, the press a route on the input router under the action the caller names.</summary>
public sealed class PanGesture(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/PanGesture.js")
{
    /// <summary>Attaches panning to the container, its press routed as <paramref name="action"/>, active only while the container carries <paramref name="whileClass"/>.</summary>
    public ValueTask<bool> AttachAsync(ElementReference container, string action, string whileClass)
        => AttachCoreAsync(container, new { action, whileClass });
}
