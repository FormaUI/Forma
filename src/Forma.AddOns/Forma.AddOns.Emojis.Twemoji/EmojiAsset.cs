namespace Forma.Framework.Emojis.Twemoji;

using Forma.Framework.Emojis;

/// <summary>One emoji as the embedded catalog holds it.</summary>
internal sealed record EmojiAsset
{
    /// <summary>The emoji itself.</summary>
    public required Emoji Emoji { get; init; }

    /// <summary>The emoji's English name.</summary>
    public required string Name { get; init; }

    /// <summary>The keywords a search matches this emoji by, beyond its name.</summary>
    public required IReadOnlyList<string> Keywords { get; init; }

    /// <summary>The identifier of the group this emoji belongs to.</summary>
    public required string GroupId { get; init; }

    /// <summary>The identifier of this emoji's symbol within its group's sprite.</summary>
    public required string SpriteId { get; init; }

    /// <summary>The index of the group's sprite sheet that carries this emoji's symbol.</summary>
    public required int ChunkIndex { get; init; }
}
