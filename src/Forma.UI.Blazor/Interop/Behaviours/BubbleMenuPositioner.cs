namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Positions a floating bubble menu above or below an active text selection.</summary>
internal sealed class BubbleMenuPositioner(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/behaviours/BubbleMenuPositioner.js")
{
    /// <summary>Attaches the positioner to the menu element.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target, ElementReference? editor = null, string? activeClass = null)
        => AttachCoreAsync(target, editor, new { activeClass });

    /// <summary>Updates the bubble menu position relative to the active selection.</summary>
    public ValueTask<bool> UpdateAsync()
        => InvokeAsync("update");
}
