namespace Forma.Framework.Emojis;

using System.Diagnostics.CodeAnalysis;

/// <summary>The emoji a host offers, browsable by category and searchable by name and keyword.</summary>
public interface IEmojiCatalog
{
    /// <summary>Finds the emoji filed under a group.</summary>
    /// <param name="groupId">The identifier of the group.</param>
    /// <param name="emojis">The emoji filed under the group.</param>
    /// <returns>Whether the group is known.</returns>
    bool TryFindByGroupId(string groupId, out IReadOnlyList<EmojiInfo> emojis);

    /// <summary>Finds the catalog entry of an emoji.</summary>
    /// <param name="emoji">The emoji to look up.</param>
    /// <param name="info">The emoji's catalog entry.</param>
    /// <returns>Whether the emoji is known.</returns>
    bool TryGetByEmoji(Emoji emoji, [MaybeNullWhen(false)] out EmojiInfo info);

    /// <summary>Finds the emoji whose name or keywords match every whitespace-separated term.</summary>
    /// <param name="text">The search text.</param>
    /// <returns>The matching emoji.</returns>
    IReadOnlyList<EmojiInfo> Find(string text);
}
