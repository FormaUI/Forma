namespace Forma.Framework.Emojis;

using System.Diagnostics.CodeAnalysis;

/// <summary>Artwork drawn from a standalone image.</summary>
/// <param name="Url">The address of the image.</param>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record EmojiImage(string Url) : EmojiArtwork;
