namespace Forma.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Thumbnails rendered from the browser's own File objects, held for a container of image elements.</summary>
internal sealed class FilePreview(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Blazor/scripts/behaviours/FilePreview.js")
{
    /// <summary>Attaches the preview to a container holding a file input and its preview images.</summary>
    public ValueTask<bool> AttachAsync(ElementReference container)
        => AttachCoreAsync(container);

    /// <summary>Renders one thumbnail per name, matching each against the file input's current selection.</summary>
    public ValueTask<bool> ShowAsync(IReadOnlyList<string> names)
        => InvokeAsync("show", names);
}
