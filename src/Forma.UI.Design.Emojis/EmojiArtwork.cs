namespace Forma.UI.Design.Emojis;

using System.Diagnostics.CodeAnalysis;

/// <summary>The artwork used to draw an emoji.</summary>
public record EmojiArtwork
{
    /// <summary>Drawn by the platform's own emoji font.</summary>
    public static EmojiArtwork Unicode { get; } = new();

    /// <summary>Drawn from a symbol in a sheet that must be loaded before it resolves.</summary>
    /// <param name="symbol">The identifier of the symbol within the sheet.</param>
    /// <param name="sheet">The address of the sheet the symbol is loaded from.</param>
    public static EmojiArtwork Sprite(string symbol, string sheet)
        => new EmojiSprite(symbol, sheet);

    /// <summary>Drawn from a standalone image.</summary>
    /// <param name="url">The address of the image.</param>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
    public static EmojiArtwork Image(string url)
        => new EmojiImage(url);

    /// <summary>Closes the hierarchy to the artwork kinds this project defines.</summary>
    private protected EmojiArtwork()
    {
    }
}
