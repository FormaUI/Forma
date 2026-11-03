namespace Forma.Framework.Browser.Interop.Behaviors;

using System.Diagnostics.CodeAnalysis;

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

        return AttachCoreAsync(target, new AttachOptions<TOwner> { Action = wheel.Action, Owner = owner, Modifier = GetModifierName(wheel.Modifier) });
    }

    private static string? GetModifierName(WheelModifier modifier) => modifier switch
    {
        WheelModifier.Shift => "shift",
        WheelModifier.Ctrl => "ctrl",
        WheelModifier.Alt => "alt",
        _ => null,
    };

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required string Action { get; init; }

        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required string? Modifier { get; init; }
    }
}
