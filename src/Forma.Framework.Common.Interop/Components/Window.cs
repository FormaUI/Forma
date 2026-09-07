namespace Forma.Framework.Common.Interop.Components;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a floating window: moving it by its title bar and resizing it at its edges, reporting where it came to rest.</summary>
public sealed class Window(IJSRuntime js)
    : JsInterop(js, "./_content/forma/common/scripts/components/Window.js")
{
    /// <summary>Attaches to the window, dragging it by the element carrying <paramref name="titleBarClass"/>, which also takes the arrow keys while focused and steps the window by <paramref name="stepPixels"/>, and resizing it by the elements carrying <paramref name="handleClass"/>, telling <paramref name="owner"/> where it came to rest.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference window, DotNetObjectReference<TOwner> owner, string titleBarClass, string handleClass, double stepPixels)
        where TOwner : class
        => AttachCoreAsync(window, new { owner, titleBarClass, handleClass, stepPixels });

    /// <summary>Tells the owner where the window stands now.</summary>
    public ValueTask<bool> MeasureAsync()
        => InvokeAsync("measure");
}
