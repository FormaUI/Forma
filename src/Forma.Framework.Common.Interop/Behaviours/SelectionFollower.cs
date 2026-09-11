namespace Forma.Framework.Common.Interop.Behaviours;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Where a following element sits relative to the selection.</summary>
public enum SelectionFollowerPlacement
{
    /// <summary>Beneath the selection, moving above it only when the viewport leaves no room beneath.</summary>
    Below,

    /// <summary>Over the selection, moving beneath it only when the viewport leaves no room above.</summary>
    Above,
}

/// <summary>Keeps an element positioned beside the text selection inside its owner, holding focus while it shows.</summary>
public sealed class SelectionFollower(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/common/scripts/behaviours/SelectionFollower.js")
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
            // An owner named here must arrive in the browser; a reference that resolves to nothing hides the element rather than widening it to every selection.
            requiresOwner = owner is not null,
        });

    /// <summary>Moves the element to the current selection.</summary>
    public ValueTask<bool> UpdateAsync()
        => InvokeAsync("update");
}
