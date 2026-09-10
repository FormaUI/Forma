namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A region of the page. Its <see cref="Variant"/> decides its look: flat, raised, inset or canvas. It draws no line.</summary>
public sealed partial class FormaBackdrop : FormaElementBase
{
    /// <summary>The look the backdrop takes: flat, raised, inset or canvas.</summary>
    [Parameter] public BackdropVariant Variant { get; set; } = BackdropVariant.Flat;

    /// <summary>What the backdrop holds.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-backdrop")
            .Add(Variant switch
            {
                BackdropVariant.Raised => "forma-backdrop-raised",
                BackdropVariant.Inset => "forma-backdrop-inset",
                BackdropVariant.Canvas => "forma-backdrop-canvas",
                _ => null,
            });
}
