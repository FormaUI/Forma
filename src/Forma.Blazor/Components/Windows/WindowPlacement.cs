namespace Forma.Blazor.Components.Windows;

/// <summary>Where a floating <see cref="FormaWindow"/> stands in the viewport and how large it is, in CSS pixels.</summary>
/// <param name="Left">The distance from the viewport's left edge.</param>
/// <param name="Top">The distance from the viewport's top edge.</param>
/// <param name="Width">The window's width.</param>
/// <param name="Height">The window's height.</param>
public sealed record WindowPlacement(double Left, double Top, double Width, double Height);
