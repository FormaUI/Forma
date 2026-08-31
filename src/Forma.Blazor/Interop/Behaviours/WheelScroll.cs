namespace Forma.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Moves something sideways with a wheel that only reports vertically.</summary>
internal sealed class WheelScroll(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Blazor/scripts/behaviours/WheelScroll.js")
{
    /// <summary>Attaches to the bar, whose track then answers a wheel turn anywhere over it until it reaches either end.</summary>
    public ValueTask<bool> AttachAsync(ElementReference bar, ElementReference track)
        => AttachCoreAsync(bar, new { track });

    /// <summary>Attaches to the target, invoking <c>HandleWheelAsync</c> on <paramref name="owner"/> with the direction of each wheel gesture over it, or of each made with <paramref name="modifier"/> held.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner, WheelModifier modifier = WheelModifier.None)
        where TOwner : class
        => AttachCoreAsync(target, new { owner, modifier = ModifierName(modifier) });

    private static string? ModifierName(WheelModifier modifier) => modifier switch
    {
        WheelModifier.Shift => "shift",
        WheelModifier.Ctrl => "ctrl",
        WheelModifier.Alt => "alt",
        _ => null
    };
}
