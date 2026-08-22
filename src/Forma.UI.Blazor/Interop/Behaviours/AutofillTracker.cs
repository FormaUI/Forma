namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Marks a field the browser filled in without the user typing.</summary>
internal sealed class AutofillTracker(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/AutofillTracker.js")
{
    /// <summary>Attaches tracking to the field.</summary>
    public ValueTask<bool> AttachAsync(ElementReference input)
        => AttachCoreAsync(input);
}
