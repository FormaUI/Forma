namespace Forma.UI.Design.Icons;

/// <summary>Artwork drawn as SVG geometry.</summary>
/// <param name="ViewBox">The SVG coordinate system the paths are drawn in.</param>
/// <param name="Paths">The paths composing the artwork, in painting order.</param>
public record IconVector(string ViewBox, IReadOnlyList<IconPath> Paths) : IconArtwork;
