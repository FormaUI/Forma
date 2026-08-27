namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Scrolls a sideways track with a wheel that only reports vertically.</summary>
internal sealed class WheelScroll(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/WheelScroll.js")
{
    /// <summary>Attaches to the track, which then answers a wheel turn until it reaches either end.</summary>
    public ValueTask<bool> AttachAsync(ElementReference track)
        => AttachCoreAsync(track);
}
