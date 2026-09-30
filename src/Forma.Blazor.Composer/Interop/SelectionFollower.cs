namespace Forma.Blazor.Components.Composer.Interop;

using Forma.Framework.JavaScript.Interop;

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
internal sealed class SelectionFollower(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.Composer/scripts/SelectionFollower.js")
{
    /// <summary>Attaches to the element that follows the selection.</summary>
    /// <remarks>The active class marks the element while a selection inside the owner is active, and the handle moves it by dragging or by the arrow keys.</remarks>
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
            // An owner that resolves to nothing in the browser hides the element.
            requiresOwner = owner is not null,
        });

    /// <summary>Moves the element to the current selection.</summary>
    public ValueTask<bool> UpdateAsync()
        => InvokeAsync("update");
}
