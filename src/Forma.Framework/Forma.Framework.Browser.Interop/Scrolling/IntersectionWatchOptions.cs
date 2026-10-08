namespace Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>What an observation of an element's <see cref="IntersectionState"/> measures it against.</summary>
public sealed record IntersectionWatchOptions
{
    /// <summary>The scroll container the element comes into view of; <see langword="null"/> is the viewport.</summary>
    public ElementReference? Root { get; init; }

    /// <summary>How far beyond the root's edges the element counts as in view, in pixels.</summary>
    public double RootMarginPixels { get; init; }
}
