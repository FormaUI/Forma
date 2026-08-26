namespace Forma.UI.Design.Icons;

/// <summary>Provides artwork for semantic icons.</summary>
public interface IIconProvider
{
    /// <summary>Gets the artwork used to render an icon.</summary>
    /// <param name="icon">The icon to draw.</param>
    /// <returns>The artwork for the icon.</returns>
    IconArtwork GetArtwork(Icon icon);
}
