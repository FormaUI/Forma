namespace Forma.Blazor.Components.Layout;

/// <summary>Where a positioned <see cref="FormaFrame"/> stands in the viewport and how large it is, in CSS pixels.</summary>
/// <param name="Left">The distance from the viewport's left edge.</param>
/// <param name="Top">The distance from the viewport's top edge.</param>
/// <param name="Width">The frame's width.</param>
/// <param name="Height">The frame's height.</param>
public sealed record FramePlacement(double Left, double Top, double Width, double Height);
