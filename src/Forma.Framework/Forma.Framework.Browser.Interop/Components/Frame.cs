namespace Forma.Framework.Browser.Interop.Components;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a frame: a box moved by the frame's drag region, resized at its edges, stepped by the arrow keys on the focused drag region and held within its bounds; the box is the frame's own element or the parent that presents it.</summary>
public sealed class Frame(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/components/Frame.js")
{
    /// <summary>Attaches to the frame. Its box is the frame itself when <paramref name="box"/> is <c>self</c> and its parent when <c>parent</c>. The box is dragged by the element carrying <paramref name="dragRegionClass"/>, which also takes the arrow keys while focused and steps the box by <paramref name="stepPixels"/>, resized by the elements carrying <paramref name="handleClass"/>, and held within <paramref name="bounds"/> or the viewport. <paramref name="owner"/> is told where the box came to rest.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(
        ElementReference frame,
        DotNetObjectReference<TOwner> owner,
        string box,
        string dragRegionClass,
        string handleClass,
        double stepPixels,
        ElementReference? bounds)
        where TOwner : class
        => AttachCoreAsync(frame, new { owner, box, dragRegionClass, handleClass, stepPixels, bounds });

    /// <summary>Tells the owner where the box stands now.</summary>
    public ValueTask<bool> MeasureAsync()
        => InvokeAsync("measure");

    /// <summary>Holds the box within its bounds again.</summary>
    public ValueTask<bool> FitAsync()
        => InvokeAsync("fit");
}
