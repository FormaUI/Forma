namespace Forma.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Stops Enter from submitting the form around a control.</summary>
internal sealed class ImplicitSubmitGuard(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Blazor/scripts/behaviours/ImplicitSubmitGuard.js")
{
    /// <summary>Attaches the guard to the control.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target)
        => AttachCoreAsync(target);
}
