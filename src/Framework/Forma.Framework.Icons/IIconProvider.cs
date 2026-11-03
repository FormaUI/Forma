namespace Forma.Framework.Icons;

/// <summary>Provides artwork for icons.</summary>
public interface IIconProvider
{
    /// <summary>Attempts to get the artwork used to render an icon at a size.</summary>
    /// <param name="icon">The icon to draw.</param>
    /// <param name="size">The size the icon is drawn at.</param>
    /// <param name="artwork">The artwork for the icon.</param>
    /// <returns>Whether the provider has artwork for the icon.</returns>
    bool TryGetArtwork(Icon icon, IconSize size, out IconArtwork artwork);

    /// <summary>Gets the artwork used to render an icon at a size.</summary>
    /// <param name="icon">The icon to draw.</param>
    /// <param name="size">The size the icon is drawn at.</param>
    /// <returns>The artwork for the icon.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The icon has no artwork.
    /// </exception>
    IconArtwork GetArtwork(Icon icon, IconSize size);
}
