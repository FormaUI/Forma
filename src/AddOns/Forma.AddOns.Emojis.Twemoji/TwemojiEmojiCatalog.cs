namespace Forma.Framework.Emojis.Twemoji;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Emojis;

/// <summary>The catalog of the emoji this package ships, grouped by category and searchable by name and keyword.</summary>
public sealed class TwemojiEmojiCatalog : IEmojiCatalog
{
    private readonly Lazy<IReadOnlyDictionary<string, EmojiInfo>> _bySequence = new(BuildIndex);

    /// <inheritdoc />
    public bool TryFindByGroupId(string groupId, out IReadOnlyList<EmojiInfo> emojis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        if (!EmojiAssetLoader.IsKnownGroup(groupId))
        {
            emojis = [];
            return false;
        }

        emojis = [.. EmojiAssetLoader.LoadGroup(groupId).Select(ToInfo)];
        return true;
    }

    /// <inheritdoc />
    public bool TryGetByEmoji(Emoji emoji, [MaybeNullWhen(false)] out EmojiInfo info)
        => _bySequence.Value.TryGetValue(emoji.Sequence, out info);

    /// <inheritdoc />
    public IReadOnlyList<EmojiInfo> Find(string text)
    {
        var terms = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (terms.Length == 0)
        {
            return [];
        }

        return [.. _bySequence.Value.Values.Where(info => terms.All(term => Matches(info, term)))];
    }

    private static bool Matches(EmojiInfo info, string term)
        => StartsAWord(info.Name, term) || info.Keywords.Any(keyword => StartsAWord(keyword, term));

    // Whether any space-separated word of the text starts with the term.
    private static bool StartsAWord(string text, string term)
    {
        var index = 0;

        while (index <= text.Length - term.Length)
        {
            if (string.Compare(text, index, term, 0, term.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            var nextSpaceIndex = text.IndexOf(' ', index, StringComparison.Ordinal);
            if (nextSpaceIndex < 0)
            {
                return false;
            }

            index = nextSpaceIndex + 1;
        }

        return false;
    }

    private static EmojiInfo ToInfo(EmojiAsset asset)
        => new()
        {
            Emoji = asset.Emoji,
            Name = asset.Name,
            Keywords = asset.Keywords,
            GroupId = asset.GroupId,
        };

    private static Dictionary<string, EmojiInfo> BuildIndex()
        => EmojiAssetLoader.LoadAll().Select(ToInfo).ToDictionary(info => info.Emoji.Sequence, StringComparer.Ordinal);
}
