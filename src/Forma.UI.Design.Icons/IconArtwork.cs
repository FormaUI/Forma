namespace Forma.UI.Design.Icons;

/// <summary>The artwork used to draw an icon.</summary>
public record IconArtwork
{
    /// <summary>Nothing to draw, for a glyph the provider cannot depict.</summary>
    public static IconArtwork None { get; } = new();

    /// <summary>Drawn as SVG geometry.</summary>
    /// <param name="viewBox">The SVG coordinate system the paths are drawn in.</param>
    /// <param name="paths">The paths composing the artwork, in painting order.</param>
    public static IconArtwork Vector(string viewBox, IReadOnlyList<IconPath> paths)
        => new IconVector(viewBox, paths);

    /// <summary>Drawn as a character by whichever font the reader's platform resolves it with.</summary>
    /// <param name="text">The character sequence depicting the glyph.</param>
    public static IconArtwork Character(string text)
        => new IconCharacter(text);

    /// <summary>Closes the hierarchy to the artwork kinds this project defines.</summary>
    private protected IconArtwork()
    {
    }
}
