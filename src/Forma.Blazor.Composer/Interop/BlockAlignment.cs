namespace Forma.Blazor.Components.Composer.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Keeps the rows of an element level with the blocks of another, as those blocks move with typing, resizing and scrolling.</summary>
internal sealed class BlockAlignment(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.Composer/scripts/BlockAlignment.js")
{
    /// <summary>Attaches to the element whose rows follow the blocks, which are the source's children; each row names its block's index in the attribute.</summary>
    /// <param name="target">The element holding the rows.</param>
    /// <param name="source">The element whose children are the blocks.</param>
    /// <param name="rowSelector">A selector finding the rows inside the target.</param>
    /// <param name="blockAttribute">The attribute on a row carrying its block's zero-based index.</param>
    public ValueTask<bool> AttachAsync(ElementReference target, ElementReference source, string rowSelector, string blockAttribute = "data-block")
        => AttachCoreAsync(target, new { source, rowSelector, blockAttribute });
}
