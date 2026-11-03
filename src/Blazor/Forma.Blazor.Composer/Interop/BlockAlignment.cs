namespace Forma.Blazor.Components.Composer.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Keeps the rows of an element level with the blocks of another.</summary>
/// <remarks>The rows follow the blocks through typing, resizing and scrolling.</remarks>
internal sealed class BlockAlignment(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.Composer/scripts/BlockAlignment.js")
{
    /// <summary>Attaches to the element whose rows follow the source's blocks.</summary>
    /// <param name="target">The element holding the rows.</param>
    /// <param name="source">The element whose children are the blocks.</param>
    /// <param name="rowSelector">The selector of the rows inside the target.</param>
    /// <param name="blockAttribute">The attribute on a row carrying its block's zero-based index.</param>
    public ValueTask<bool> AttachAsync(ElementReference target, ElementReference source, string rowSelector, string blockAttribute = "data-block")
        => AttachCoreAsync(target, new AttachOptions { Source = source, RowSelector = rowSelector, BlockAttribute = blockAttribute });

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions
    {
        public required ElementReference Source { get; init; }

        public required string RowSelector { get; init; }

        public required string BlockAttribute { get; init; }
    }
}
