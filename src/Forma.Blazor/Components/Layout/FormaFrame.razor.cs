namespace Forma.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>A region of the page. Its <see cref="Variant"/> decides its look: flat, raised, inset or canvas. It draws no line.</summary>
public sealed partial class FormaFrame : ComponentBase
{
    /// <summary>The look the frame takes: flat, raised, inset or canvas.</summary>
    [Parameter] public FrameVariant Variant { get; set; } = FrameVariant.Flat;

    /// <summary>What the frame holds.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes applied to the frame element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
        => AdditionalAttributes.MergeClass(
            "forma-frame",
            Variant switch
            {
                FrameVariant.Raised => "forma-frame-raised",
                FrameVariant.Inset => "forma-frame-inset",
                FrameVariant.Canvas => "forma-frame-canvas",
                _ => null,
            });
}
