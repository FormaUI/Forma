namespace Forma.Framework.Emojis;

using System.Diagnostics.CodeAnalysis;

/// <summary>A catalog holding no emoji.</summary>
public sealed class NullEmojiCatalog : IEmojiCatalog
{
    /// <summary>The single instance.</summary>
    public static NullEmojiCatalog Instance { get; } = new();

    private NullEmojiCatalog()
    {
    }

    /// <inheritdoc />
    public bool TryFindByGroupId(string groupId, out IReadOnlyList<EmojiInfo> emojis)
    {
        emojis = [];
        return false;
    }

    /// <inheritdoc />
    public bool TryGetByEmoji(Emoji emoji, [MaybeNullWhen(false)] out EmojiInfo info)
    {
        info = null;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<EmojiInfo> Find(string text)
        => [];
}
