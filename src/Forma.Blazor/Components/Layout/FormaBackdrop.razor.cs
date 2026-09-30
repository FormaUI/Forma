namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A region of the page in the look its <see cref="Variant"/> names.</summary>
public sealed partial class FormaBackdrop : FormaElementBase
{
    /// <summary>The look the backdrop takes: flat, raised, inset or canvas.</summary>
    [Parameter] public BackdropVariant Variant { get; set; } = BackdropVariant.Flat;

    /// <summary>The content the backdrop holds.</summary>
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
