namespace Forma.UI.Design.Icons;

/// <summary>The SVG artwork used to render an icon.</summary>
/// <param name="ViewBox">The SVG coordinate system the paths are drawn in.</param>
/// <param name="Paths">The paths composing the artwork, in painting order.</param>
public sealed record IconArtwork(string ViewBox, IReadOnlyList<IconPath> Paths);
