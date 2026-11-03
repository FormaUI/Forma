namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A layout of backdrops along one axis, one seam apart.</summary>
/// <remarks>The seam is one spacing step of the parent showing between two backdrops; a <see cref="FormaSplitter"/> placed between them stands in it.</remarks>
public sealed partial class FormaStackLayout : FormaElementBase
{
    /// <summary>The axis the backdrops are stacked along.</summary>
    [Parameter] public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;

    /// <summary>The backdrops the layout stacks, and the splitters between them.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-stack-layout")
            .Add(Orientation is StackOrientation.Horizontal ? "forma-stack-layout-horizontal" : "forma-stack-layout-vertical");
}
