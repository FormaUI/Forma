namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Where a following element sits relative to the selection.</summary>
internal enum SelectionFollowerPlacement
{
    /// <summary>Beneath the selection, moving above it only when the viewport leaves no room beneath.</summary>
    Below,

    /// <summary>Over the selection, moving beneath it only when the viewport leaves no room above.</summary>
    Above,
}

/// <summary>Keeps an element positioned beside the text selection inside its owner, holding focus while it shows.</summary>
internal sealed class SelectionFollower(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/SelectionFollower.js")
{
    /// <summary>Attaches to the element that follows the selection; a class marks it while a selection inside the owner is active, and a handle inside it drags or arrow-keys it to a new place.</summary>
    public ValueTask<bool> AttachAsync(
        ElementReference target,
        ElementReference? owner = null,
        string? activeClass = null,
        SelectionFollowerPlacement placement = SelectionFollowerPlacement.Below,
        string? handleSelector = null)
        => AttachCoreAsync(target, owner, new
        {
            activeClass,
            placement = placement == SelectionFollowerPlacement.Above ? "above" : "below",
            handleSelector,
        });

    /// <summary>Moves the element to the current selection.</summary>
    public ValueTask<bool> UpdateAsync()
        => InvokeAsync("update");
}
