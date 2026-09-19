namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Marks a field the browser filled in without the user typing.</summary>
public sealed class AutofillTracker(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/AutofillTracker.js")
{
    /// <summary>Attaches tracking to the field.</summary>
    public ValueTask<bool> AttachAsync(ElementReference input)
        => AttachCoreAsync(input);
}
