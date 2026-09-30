namespace Forma.Framework.Emojis.Twemoji;

using Forma.Framework.Emojis;

/// <summary>Provides the Twemoji artwork this package ships, from its embedded sprite sheets.</summary>
public sealed class TwemojiEmojiProvider : IEmojiProvider
{
    // Variation Selector-16, the emoji presentation selector.
    private const string EmojiPresentation = "\uFE0F";

    private readonly Lazy<IReadOnlyDictionary<string, EmojiAsset>> _bySequence = new(BuildIndex);

    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
    {
        if (emoji.Sequence is null)
        {
            throw new ArgumentOutOfRangeException(nameof(emoji), "An emoji naming no sequence has no artwork.");
        }

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

        // Each emoji is also indexed under its unqualified spelling, its sequence without presentation selectors.
        foreach (var asset in assets)
        {
            index.TryAdd(asset.Emoji.Sequence.Replace(EmojiPresentation, string.Empty, StringComparison.Ordinal), asset);
        }

        return index;
    }
}
