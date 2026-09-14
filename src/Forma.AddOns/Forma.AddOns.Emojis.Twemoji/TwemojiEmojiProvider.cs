namespace Forma.Framework.Emojis.Twemoji;

using Forma.Framework.Emojis;

/// <summary>Provides the Twemoji artwork this package ships, drawing an emoji from its embedded sprite sheets.</summary>
public sealed class TwemojiEmojiProvider : IEmojiProvider
{
    // Variation Selector-16, which a fully-qualified emoji spells its presentation with.
    private const string EmojiPresentation = "\uFE0F";

    private readonly Lazy<IReadOnlyDictionary<string, EmojiAsset>> _bySequence = new(BuildIndex);

    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
    {
        if (emoji.Sequence is null)
        {
            throw new ArgumentOutOfRangeException(nameof(emoji), "An emoji naming no sequence has no artwork.");
        }

        // An emoji outside the embedded set is still real data a reader may have pasted in,
        // so it degrades to the platform's own rendering rather than failing to draw at all.
        if (!_bySequence.Value.TryGetValue(emoji.Sequence, out var asset))
        {
            return EmojiArtwork.Unicode;
        }

        return EmojiArtwork.Sprite(
            symbol: $"emoji-{asset.SpriteId}",
            sheet: $"{EmojiAssetLoader.SpriteBasePath}/{asset.GroupId}-{asset.ChunkIndex}.svg");
    }

    private static Dictionary<string, EmojiAsset> BuildIndex()
    {
        var assets = EmojiAssetLoader.LoadAll();
        var index = new Dictionary<string, EmojiAsset>(assets.Count, StringComparer.Ordinal);

        foreach (var asset in assets)
        {
            index[asset.Emoji.Sequence] = asset;
        }

        // An unqualified spelling differs from the catalog's fully-qualified one only by its emoji
        // presentation selectors, and both name one emoji — so a pasted "⚠" still draws "⚠️".
        foreach (var asset in assets)
        {
            index.TryAdd(asset.Emoji.Sequence.Replace(EmojiPresentation, string.Empty, StringComparison.Ordinal), asset);
        }

        return index;
    }
}
