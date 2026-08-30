namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Keeps an element positioned over the text selection inside its owner, holding focus while it shows.</summary>
internal sealed class SelectionFollower(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/SelectionFollower.js")
{
    /// <summary>Attaches to the element that follows the selection; a class marks it while a selection inside the owner is active.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target, ElementReference? owner = null, string? activeClass = null)
        => AttachCoreAsync(target, owner, new { activeClass });

    /// <summary>Moves the element to the current selection.</summary>
    public ValueTask<bool> UpdateAsync()
        => InvokeAsync("update");
}
