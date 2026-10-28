namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>The scope of a router's render tree, within which a <see cref="CultureRoutePage"/> an interactive router renders follows its route.</summary>
/// <remarks>
/// The scope cascades as a fixed value, which stops where the render mode changes, so it reaches every page the router
/// renders and never the interactive instance of a page that is an island of its own.
/// </remarks>
public sealed class CultureRouteScope : ComponentBase
{
    /// <summary>The router the scope surrounds.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.OpenComponent<CascadingValue<CultureRouteScope>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
