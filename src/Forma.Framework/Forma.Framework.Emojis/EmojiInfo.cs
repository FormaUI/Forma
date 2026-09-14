namespace Forma.Framework.Emojis;

/// <summary>One emoji in a catalog, with the terms a search matches it by.</summary>
public sealed record EmojiInfo
{
    /// <summary>The emoji itself.</summary>
    public required Emoji Emoji { get; init; }

    /// <summary>The emoji's English name.</summary>
    public required string Name { get; init; }

    /// <summary>The terms a search matches this emoji by, beyond its name.</summary>
    public required IReadOnlyList<string> Keywords { get; init; }

    /// <summary>The identifier of the group this emoji belongs to.</summary>
    public required string GroupId { get; init; }
}
