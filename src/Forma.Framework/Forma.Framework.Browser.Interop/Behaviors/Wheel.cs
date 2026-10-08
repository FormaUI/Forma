namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Answers a wheel turned over a surface by telling an owner which way it turned.</summary>
public sealed class Wheel(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/Wheel.js")
{
    /// <summary>Attaches to the surface, telling the owner each direction the wheel turns through <c>HandleWheelAsync</c>.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, ScrollWheel wheel, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(wheel);
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, new { action = wheel.Action, owner, modifier = GetModifierName(wheel.Modifier) });
    }

    private static string? GetModifierName(WheelModifier modifier) => modifier switch
    {
        WheelModifier.Shift => "shift",
        WheelModifier.Ctrl => "ctrl",
        WheelModifier.Alt => "alt",
        _ => null,
    };
}
