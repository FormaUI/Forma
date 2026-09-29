namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Stops Enter from submitting the form around a control, or around the controls a selector names within a surface.</summary>
public sealed class ImplicitSubmitGuard(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/ImplicitSubmitGuard.js")
{
    /// <summary>Attaches the guard to the control, or to the surface whose controls matching <paramref name="within"/> it guards.</summary>
    /// <param name="target">The control, or the surface holding the controls.</param>
    /// <param name="within">A selector naming the controls within the surface the guard covers; every element when omitted.</param>
    public ValueTask<bool> AttachAsync(ElementReference target, string? within = null)
        => AttachCoreAsync(target, new { within });
}
