namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A drop zone that hands dropped files to the file input inside an element.</summary>
internal sealed class FileDropZone(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/FileDropZone.js")
{
    /// <summary>Attaches the drop zone, marking the element with <paramref name="activeClass"/> while a drag is over it.</summary>
    public ValueTask<bool> AttachAsync(ElementReference dropZone, string activeClass)
        => AttachCoreAsync(dropZone, new { activeClass });
}
