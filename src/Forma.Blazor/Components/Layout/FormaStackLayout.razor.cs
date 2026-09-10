namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>Lays backdrops along one axis, one seam apart. The seam is one spacing step of the parent showing between them; a <see cref="FormaSplitter"/> placed between two backdrops stands in it.</summary>
public sealed partial class FormaStackLayout : FormaElementBase
{
    /// <summary>The axis the backdrops are stacked along.</summary>
    [Parameter] public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;

    /// <summary>The backdrops, with a <see cref="FormaSplitter"/> between two that resize.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-stack-layout")
            .Add(Orientation is StackOrientation.Horizontal ? "forma-stack-layout-horizontal" : "forma-stack-layout-vertical");
}
