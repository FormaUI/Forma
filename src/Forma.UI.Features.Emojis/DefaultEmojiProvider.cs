namespace Forma.UI.Features.Emojis;

using Forma.UI.Design.Emojis;

/// <summary>Provides the artwork this library ships, drawing an emoji from its embedded sprite sheets.</summary>
public sealed class DefaultEmojiProvider : IEmojiProvider
{
    private readonly Lazy<IReadOnlyDictionary<string, EmojiAsset>> _byCharacter = new(BuildIndex);

    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
    {
        if (emoji.Character is null)
        {
            throw new ArgumentOutOfRangeException(nameof(emoji), "An emoji naming no character has no artwork.");
        }

        // An emoji outside the embedded set is still real data a reader may have pasted in,
        // so it degrades to the platform's own rendering rather than failing to draw at all.
        if (!_byCharacter.Value.TryGetValue(emoji.Character, out var asset))
        {
            return EmojiArtwork.Unicode;
        }

        return EmojiArtwork.Sprite(
            symbol: $"emoji-{asset.SpriteId}",
            sheet: $"{EmojiAssetLoader.SpriteBasePath}/{asset.GroupId}-{asset.ChunkIndex}.svg");
    }

    private static Dictionary<string, EmojiAsset> BuildIndex()
        => EmojiAssetLoader.All().ToDictionary(asset => asset.Emoji.Character, StringComparer.Ordinal);
}
