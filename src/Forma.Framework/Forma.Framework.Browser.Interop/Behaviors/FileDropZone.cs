namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A drop zone that hands dropped files to the file input inside an element, the drop a route on the input router under the action the caller names.</summary>
public sealed class FileDropZone(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/FileDropZone.js")
{
    /// <summary>Attaches the drop zone, its drop routed as <paramref name="action"/>, marking the element with <paramref name="activeClass"/> while a drag is over it.</summary>
    public ValueTask<bool> AttachAsync(ElementReference dropZone, string action, string activeClass)
        => AttachCoreAsync(dropZone, new { action, activeClass });
}
