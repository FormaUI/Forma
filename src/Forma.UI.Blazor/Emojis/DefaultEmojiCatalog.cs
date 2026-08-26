namespace Forma.UI.Blazor.Emojis;

using System.Diagnostics.CodeAnalysis;

using Forma.UI.Design.Emojis;

/// <summary>Reads the emoji this library ships, grouped by category and searchable by name and keyword.</summary>
public sealed class DefaultEmojiCatalog : IEmojiCatalog
{
    private readonly Lazy<IReadOnlyDictionary<string, EmojiInfo>> _byCharacter = new(BuildIndex);

    /// <inheritdoc />
    public bool TryFindByGroupId(string groupId, out IReadOnlyList<EmojiInfo> emojis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        if (!EmojiAssetLoader.IsKnownGroup(groupId))
        {
            emojis = [];
            return false;
        }

        emojis = [.. EmojiAssetLoader.InGroup(groupId).Select(ToInfo)];
        return true;
    }

    /// <inheritdoc />
    public bool TryGetByEmoji(Emoji emoji, [MaybeNullWhen(false)] out EmojiInfo info)
        => _byCharacter.Value.TryGetValue(emoji.Character, out info);

    /// <inheritdoc />
    public IReadOnlyList<EmojiInfo> Find(string text)
    {
        var terms = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (terms.Length == 0)
        {
            return [];
        }

        return [.. _byCharacter.Value.Values.Where(info => terms.All(term => Matches(info, term)))];
    }

    private static bool Matches(EmojiInfo info, string term)
        => StartsAWord(info.Name, term) || info.Keywords.Any(keyword => StartsAWord(keyword, term));

    // Matching the start of a word rather than any substring keeps "cat" off "intoxicated".
    private static bool StartsAWord(string text, string term)
    {
        var index = 0;

        while (index <= text.Length - term.Length)
        {
            if (string.Compare(text, index, term, 0, term.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            var next = text.IndexOf(' ', index, StringComparison.Ordinal);
            if (next < 0)
            {
                return false;
            }

            index = next + 1;
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
        => EmojiAssetLoader.All().Select(ToInfo).ToDictionary(info => info.Emoji.Character, StringComparer.Ordinal);
}
