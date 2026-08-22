namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Drags a scroll container's contents with the pointer while a class marks the container eligible.</summary>
internal sealed class PanGesture(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/PanGesture.js")
{
    /// <summary>Attaches panning to the container, active only while it carries <paramref name="whileClass"/>.</summary>
    public ValueTask<bool> AttachAsync(ElementReference container, string whileClass)
        => AttachCoreAsync(container, new { whileClass });
}
