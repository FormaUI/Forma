namespace Forma.Framework.Icons;

/// <summary>A brand mark drawn as SVG geometry, carrying the brand's name.</summary>
/// <param name="ViewBox">The SVG coordinate system the paths are drawn in.</param>
/// <param name="Paths">The paths composing the mark, in painting order.</param>
/// <param name="Title">The brand's name, as the mark's accessible name.</param>
public sealed record BrandIconArtwork(string ViewBox, IReadOnlyList<IconPath> Paths, string Title) : IconVector(ViewBox, Paths);
