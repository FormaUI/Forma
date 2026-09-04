namespace Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Stops the page scrolling under a list that owns its arrow, Home and End keys.</summary>
public sealed class NavigationScrollGuard(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Interop/scripts/behaviours/NavigationScrollGuard.js")
{
    /// <summary>Attaches the guard to the list.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target)
        => AttachCoreAsync(target);
}
