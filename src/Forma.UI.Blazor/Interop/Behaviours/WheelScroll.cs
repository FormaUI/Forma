namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Moves something sideways with a wheel that only reports vertically.</summary>
internal sealed class WheelScroll(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/WheelScroll.js")
{
    /// <summary>Attaches to the bar, whose track then answers a wheel turn anywhere over it until it reaches either end.</summary>
    public ValueTask<bool> AttachAsync(ElementReference bar, ElementReference track)
        => AttachCoreAsync(bar, new { track });

    /// <summary>Attaches to the target, invoking <c>HandleWheelAsync</c> on <paramref name="owner"/> with the direction of each wheel gesture over it.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner)
        where TOwner : class
        => AttachCoreAsync(target, new { owner });
}
