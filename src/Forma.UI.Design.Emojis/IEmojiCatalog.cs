namespace Forma.UI.Design.Emojis;

using System.Diagnostics.CodeAnalysis;

/// <summary>The emoji a host offers, browsable by category and searchable by name and keyword.</summary>
public interface IEmojiCatalog
{
    /// <summary>Finds the emoji filed under a category.</summary>
    /// <param name="groupId">The identifier of the category.</param>
    /// <param name="emojis">The category's members, or empty when the category is not known.</param>
    /// <returns>Whether the category is known.</returns>
    bool TryFindByGroupId(string groupId, out IReadOnlyList<EmojiInfo> emojis);

    /// <summary>Finds an emoji by the Unicode sequence that is it.</summary>
    /// <param name="emoji">The emoji to look up.</param>
    /// <param name="info">The emoji's catalog entry, when known.</param>
    /// <returns>Whether the emoji is known.</returns>
    bool TryGetByEmoji(Emoji emoji, [MaybeNullWhen(false)] out EmojiInfo info);

    /// <summary>Finds the emoji whose name or keywords match every whitespace-separated term.</summary>
    /// <param name="text">The search text.</param>
    /// <returns>The matching emoji, or empty when nothing matches or the text is blank.</returns>
    IReadOnlyList<EmojiInfo> Find(string text);
}
