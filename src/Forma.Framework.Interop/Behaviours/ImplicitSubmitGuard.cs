namespace Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Stops Enter from submitting the form around a control.</summary>
public sealed class ImplicitSubmitGuard(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Interop/scripts/behaviours/ImplicitSubmitGuard.js")
{
    /// <summary>Attaches the guard to the control.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target)
        => AttachCoreAsync(target);
}
