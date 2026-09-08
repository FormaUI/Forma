namespace Forma.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>Lays backdrops along one axis, one seam apart. The seam is one spacing step of the parent showing between them; a <see cref="FormaSplitter"/> placed between two backdrops stands in it.</summary>
public sealed partial class FormaStackLayout : ComponentBase
{
    /// <summary>The axis the backdrops are stacked along.</summary>
    [Parameter] public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;

    /// <summary>The backdrops, with a <see cref="FormaSplitter"/> between two that resize.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes applied to the layout element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
        => AdditionalAttributes.MergeClass(
            "forma-stack-layout",
            Orientation is StackOrientation.Horizontal ? "forma-stack-layout-horizontal" : "forma-stack-layout-vertical");
}
