namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>What a surface does with scrolling: watching how far its content can still move, and asking for the next page as a sentinel comes into view; with moving a container one step or to its center beside them.</summary>
public sealed class Scroll(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/Scroll.js")
{
    /// <summary>Attaches the behaviors to <paramref name="target"/>; the <paramref name="owner"/> is told through <c>HandleOverflowAsync</c>, with a <see cref="ScrollBoundaryReport"/>, how far the target can still scroll and how far its frozen parts reach, and through <c>HandleReachedAsync</c> when the target comes into view.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, ScrollBehaviors behaviors, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(target, new
        {
            boundary = behaviors.Boundary is { } boundary ? new { owner, axis = GetAxisName(boundary.Axis), frozen = boundary.FrozenSelector } : null,
            sentinel = behaviors.Sentinel is { } sentinel ? new { owner, root = sentinel.Root } : null,
        });
    }

    /// <summary>Centers the container's scroll position over its content.</summary>
    public ValueTask<bool> ToCenterAsync(ElementReference container)
        => InvokeModuleAsync("toCenter", container);

    /// <summary>Moves the container one step along <paramref name="axis"/> in <paramref name="direction"/>, -1 toward the start and 1 toward the end: to the next of the <paramref name="stops"/> its content marks, or by most of a page when it marks none.</summary>
    public ValueTask<bool> StepAsync(ElementReference container, int direction, ScrollAxis axis = ScrollAxis.Inline, ScrollStops? stops = null)
        => InvokeModuleAsync("step", container, direction, GetAxisName(axis), stops is null ? null : new { selector = stops.Selector, inset = stops.Inset });

    private static string GetAxisName(ScrollAxis axis)
        => axis == ScrollAxis.Block ? "block" : "inline";
}
