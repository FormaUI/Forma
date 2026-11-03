namespace Forma.Framework.Icons;

using System.Text;

/// <summary>The artwork used to draw an icon.</summary>
public record IconArtwork
{
    /// <summary>Nothing to draw.</summary>
    public static IconArtwork None { get; } = new();

    /// <summary>Drawn as SVG geometry.</summary>
    /// <param name="viewBox">The SVG coordinate system the paths are drawn in.</param>
    /// <param name="paths">The paths composing the artwork, in painting order.</param>
    public static IconArtwork Vector(string viewBox, IReadOnlyList<IconPath> paths)
        => new IconVector(viewBox, paths);

    /// <summary>A brand mark drawn as SVG geometry, carrying the brand's name.</summary>
    /// <param name="viewBox">The SVG coordinate system the paths are drawn in.</param>
    /// <param name="paths">The paths composing the mark, in painting order.</param>
    /// <param name="title">The brand's name, as the mark's accessible name.</param>
    public static IconArtwork Brand(string viewBox, IReadOnlyList<IconPath> paths, string title)
        => new BrandIconArtwork(viewBox, paths, title);

    /// <summary>Drawn as a single character by the font.</summary>
    /// <param name="value">The character drawn.</param>
    public static IconArtwork Character(Rune value)
        => new IconCharacter(value);

    /// <summary>Closes the hierarchy to the artwork kinds this project defines.</summary>
    private protected IconArtwork()
    {
    }
}
