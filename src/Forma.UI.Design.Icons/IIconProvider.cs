namespace Forma.UI.Design.Icons;

/// <summary>Provides artwork for semantic icons.</summary>
public interface IIconProvider
{
    /// <summary>Attempts to get the artwork used to render an icon.</summary>
    /// <param name="icon">The icon to draw.</param>
    /// <param name="artwork">The artwork, or no artwork when unavailable.</param>
    /// <returns>Whether artwork exists for the icon exactly as requested.</returns>
    bool TryGetArtwork(Icon icon, out IconArtwork artwork);

    /// <summary>Gets the artwork used to render an icon.</summary>
    /// <param name="icon">The icon to draw.</param>
    /// <returns>The artwork for the icon.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The icon has no artwork.
    /// </exception>
    IconArtwork GetArtwork(Icon icon);
}
