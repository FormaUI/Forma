namespace Forma.Framework.Browser.Interop.Behaviors;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A drop zone that hands dropped files to the file input inside an element, the drop a route on the input router.</summary>
public sealed class FileDropZone(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/FileDropZone.js")
{
    /// <summary>Attaches the drop zone, its drop routed as <paramref name="action"/>, marking the element with <paramref name="activeClass"/> while a drag is over it.</summary>
    public ValueTask<bool> AttachAsync(ElementReference dropZone, string action, string activeClass)
        => AttachCoreAsync(dropZone, new AttachOptions { Action = action, ActiveClass = activeClass });

    /// <summary>Empties the file input, so the same file chosen again is a change.</summary>
    public ValueTask<bool> ClearAsync()
        => InvokeAsync("clear");

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions
    {
        public required string Action { get; init; }

        public required string ActiveClass { get; init; }
    }
}
