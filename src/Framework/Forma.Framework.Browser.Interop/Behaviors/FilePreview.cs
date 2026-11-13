namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Thumbnails rendered from a file input's selected files into a container's image elements.</summary>
public sealed class FilePreview(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/FilePreview.js")
{
    /// <summary>Attaches the preview to a container holding a file input and its preview images.</summary>
    public ValueTask<bool> AttachAsync(ElementReference container)
        => AttachCoreAsync(container);

    /// <summary>Renders one thumbnail per name, matching each against the file input's current selection.</summary>
    public ValueTask<bool> ShowAsync(IReadOnlyList<string> names)
        => InvokeAsync("show", names);

    /// <summary>Lets go of the thumbnails shown, releasing the addresses still held for them.</summary>
    public ValueTask<bool> ClearAsync()
        => InvokeAsync("clear");
}
