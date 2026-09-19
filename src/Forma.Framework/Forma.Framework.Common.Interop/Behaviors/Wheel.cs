namespace Forma.Framework.Common.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Answers a wheel turned over a surface: a track moves by it, or an owner is told which way it turned.</summary>
public sealed class Wheel(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/common/scripts/behaviors/Wheel.js")
{
    /// <summary>Attaches to the surface, moving the track the wheel names.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target, ScrollWheel wheel)
    {
        ArgumentNullException.ThrowIfNull(wheel);

        if (wheel.TellsOwner)
        {
            throw new ArgumentException("A wheel that tells its direction reports to an owner; attach it with one.", nameof(wheel));
        }

        return AttachCoreAsync(target, Describe<object>(wheel, null));
    }

    /// <summary>Attaches to the surface, telling the owner each direction the wheel turns through <c>HandleWheelAsync</c>.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, ScrollWheel wheel, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(wheel);
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, Describe(wheel, owner));
    }

    private static object Describe<TOwner>(ScrollWheel wheel, DotNetObjectReference<TOwner>? owner)
        where TOwner : class
        => new { track = wheel.Track, owner = wheel.TellsOwner ? owner : null, modifier = GetModifierName(wheel.Modifier) };

    private static string? GetModifierName(WheelModifier modifier) => modifier switch
    {
        WheelModifier.Shift => "shift",
        WheelModifier.Ctrl => "ctrl",
        WheelModifier.Alt => "alt",
        _ => null,
    };
}
