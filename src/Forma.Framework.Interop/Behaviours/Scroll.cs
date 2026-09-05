namespace Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>What a surface does with scrolling: watching how far its content can still move, answering a wheel, asking for the next page as a sentinel comes into view, and keeping navigation keys from scrolling the page; with moving a container one step or to its centre beside them.</summary>
public sealed class Scroll(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Interop/scripts/behaviours/Scroll.js")
{
    /// <summary>Attaches the behaviours to <paramref name="target"/> that need no owner: moving a track by the wheel, and guarding the navigation keys.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target, ScrollBehaviours behaviours)
    {
        ArgumentNullException.ThrowIfNull(behaviours);

        if (behaviours.Boundary is not null || behaviours.Sentinel is not null || behaviours.Wheel is { TellsOwner: true })
        {
            throw new ArgumentException("A boundary watch, a sentinel and a wheel that tells its direction all report to an owner; attach them with one.", nameof(behaviours));
        }

        return AttachCoreAsync(target, Describe<object>(behaviours, null));
    }

    /// <summary>Attaches the behaviours to <paramref name="target"/>; the <paramref name="owner"/> is told through <c>HandleOverflowAsync</c> how far the target can still scroll, how far its frozen parts reach in and how far the frozen corner reaches across the axis, through <c>HandleWheelAsync</c> which way a wheel turned, and through <c>HandleReachedAsync</c> when the target comes into view.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, ScrollBehaviours behaviours, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(behaviours);
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, Describe(behaviours, owner));
    }

    /// <summary>Centres the container's scroll position over its content.</summary>
    public ValueTask<bool> ToCenterAsync(ElementReference container)
        => InvokeModuleAsync("toCenter", container);

    /// <summary>Moves the container one step along <paramref name="axis"/> in <paramref name="direction"/>, -1 toward the start and 1 toward the end: to the next of the <paramref name="stops"/> its content marks, or by most of a page when it marks none.</summary>
    public ValueTask<bool> StepAsync(ElementReference container, int direction, ScrollAxis axis = ScrollAxis.Inline, ScrollStops? stops = null)
        => InvokeModuleAsync("step", container, direction, AxisName(axis), stops is null ? null : new { selector = stops.Selector, inset = stops.Inset });

    private static object Describe<TOwner>(ScrollBehaviours behaviours, DotNetObjectReference<TOwner>? owner)
        where TOwner : class
        => new
        {
            boundary = behaviours.Boundary is { } boundary ? new { owner, axis = AxisName(boundary.Axis), frozen = boundary.FrozenSelector } : null,
            wheel = behaviours.Wheel is { } wheel ? new { track = wheel.Track, owner = wheel.TellsOwner ? owner : null, modifier = ModifierName(wheel.Modifier) } : null,
            sentinel = behaviours.Sentinel is { } sentinel ? new { owner, root = sentinel.Root } : null,
            guard = behaviours.GuardsNavigation,
        };

    private static string AxisName(ScrollAxis axis)
        => axis == ScrollAxis.Block ? "block" : "inline";

    private static string? ModifierName(WheelModifier modifier) => modifier switch
    {
        WheelModifier.Shift => "shift",
        WheelModifier.Ctrl => "ctrl",
        WheelModifier.Alt => "alt",
        _ => null
    };
}
