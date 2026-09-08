namespace Forma.Framework.Common.Interop.Components;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a window: a floating one is moved by its title bar, resized at its edges and shown in the top layer within its bounds, a modal one keeps Tab inside and stands on a scrim; either kind remembers where focus was inside it and takes focus back there.</summary>
public sealed class Window(IJSRuntime js)
    : JsInterop(js, "./_content/forma/common/scripts/components/Window.js")
{
    /// <summary>Attaches to the window. Floating, it is dragged by the element carrying <paramref name="titleBarClass"/>, which also takes the arrow keys while focused and steps the window by <paramref name="stepPixels"/>, resized by the elements carrying <paramref name="handleClass"/>, held within <paramref name="bounds"/> or the viewport, and shown in the top layer while <paramref name="presented"/>; a <paramref name="modal"/> one keeps Tab inside itself and shows <paramref name="scrim"/> beneath it. <paramref name="owner"/> is told where it came to rest.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(
        ElementReference window,
        DotNetObjectReference<TOwner> owner,
        string titleBarClass,
        string handleClass,
        double stepPixels,
        bool floating,
        bool presented,
        bool modal,
        ElementReference? bounds,
        ElementReference? scrim)
        where TOwner : class
        => AttachCoreAsync(window, new { owner, titleBarClass, handleClass, stepPixels, floating, presented, modal, bounds, scrim });

    /// <summary>Tells the owner where the window stands now.</summary>
    public ValueTask<bool> MeasureAsync()
        => InvokeAsync("measure");

    /// <summary>Holds the window within its bounds again, after they or its maximization changed.</summary>
    public ValueTask<bool> FitAsync()
        => InvokeAsync("fit");

    /// <summary>Moves focus into the window unless a control inside it already holds it: to the element last focused inside it, else its first control, else the window itself.</summary>
    public ValueTask<bool> FocusAsync()
        => InvokeAsync("focus");

    /// <summary>Puts a presented floating window on top of every other floating window.</summary>
    public ValueTask<bool> RaiseAsync()
        => InvokeAsync("raise");

    /// <summary>Shows a floating window in the top layer, or takes it out while it is minimized or hidden.</summary>
    public ValueTask<bool> PresentAsync(bool presented)
        => InvokeAsync("present", presented);
}
