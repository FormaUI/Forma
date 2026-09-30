namespace Forma.Blazor.Components.Virtualization;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>One item a virtualized list draws, keyed by its owner so the element it draws is the same element for as long as the item stays drawn.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by the renderer for each item a virtualized list draws.")]
internal sealed class FormaVirtualizeItem : FormaComponentBase
{
    /// <summary>What the item draws.</summary>
    [Parameter, EditorRequired] public RenderFragment Content { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddContent(0, Content);
    }
}
