namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Arranges the Tab order inside a container: the regions named first are visited first, and the rest follows in document order.</summary>
internal sealed class FocusSequence(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/FocusSequence.js")
{
    /// <summary>Attaches to the container, visiting the controls inside each selector in turn before the rest of the container.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target, IReadOnlyList<string> order)
        => AttachCoreAsync(target, new { order });
}
