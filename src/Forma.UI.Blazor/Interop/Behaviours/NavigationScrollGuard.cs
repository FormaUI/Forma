namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Stops the page scrolling under a list that owns its arrow, Home and End keys.</summary>
internal sealed class NavigationScrollGuard(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/NavigationScrollGuard.js")
{
    /// <summary>Attaches the guard to the list.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target)
        => AttachCoreAsync(target);
}
