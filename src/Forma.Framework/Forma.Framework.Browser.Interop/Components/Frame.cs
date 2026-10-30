namespace Forma.Framework.Browser.Interop.Components;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a frame: a box moved by the frame's drag region, resized at its edges, stepped by the arrow keys on the focused drag region and held within its bounds; the box is the frame's own element or the parent that presents it.</summary>
public sealed class Frame(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/components/Frame.js")
{
    /// <summary>Attaches to the frame.</summary>
    /// <param name="frame">The frame element.</param>
    /// <param name="owner">The owner told where the box came to rest.</param>
    /// <param name="box">Which element is the box: <c>self</c> for the frame itself, <c>parent</c> for its parent.</param>
    /// <param name="dragRegionClass">The class of the element that drags the box and, while focused, steps it with the arrow keys.</param>
    /// <param name="handleClass">The class of the elements that resize the box.</param>
    /// <param name="stepPixels">The distance one arrow key steps the box, in pixels.</param>
    /// <param name="bounds">The element the box is held within; <see langword="null"/> is the viewport.</param>
    public ValueTask<bool> AttachAsync<TOwner>(
        ElementReference frame,
        DotNetObjectReference<TOwner> owner,
        string box,
        string dragRegionClass,
        string handleClass,
        double stepPixels,
        ElementReference? bounds)
        where TOwner : class
        => AttachCoreAsync(frame, new AttachOptions<TOwner>
        {
            Owner = owner,
            Box = box,
            DragRegionClass = dragRegionClass,
            HandleClass = handleClass,
            StepPixels = stepPixels,
            Bounds = bounds,
        });

    /// <summary>Tells the owner where the box stands now.</summary>
    public ValueTask<bool> MeasureAsync()
        => InvokeAsync("measure");

    /// <summary>Holds the box within its bounds again.</summary>
    public ValueTask<bool> FitAsync()
        => InvokeAsync("fit");

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required string Box { get; init; }

        public required string DragRegionClass { get; init; }

        public required string HandleClass { get; init; }

        public required double StepPixels { get; init; }

        public required ElementReference? Bounds { get; init; }
    }
}
