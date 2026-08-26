namespace Forma.UI.Features.Emojis;

using Forma.UI.Design.Emojis;

/// <summary>One emoji as the embedded catalog holds it, carrying what both the default catalog and provider need.</summary>
internal sealed record EmojiAsset
{
    /// <summary>The emoji itself.</summary>
    public required Emoji Emoji { get; init; }

    /// <summary>The emoji's English name.</summary>
    public required string Name { get; init; }

    /// <summary>The terms a search matches this emoji by, beyond its name.</summary>
    public required IReadOnlyList<string> Keywords { get; init; }

    /// <summary>The identifier of the group this emoji belongs to.</summary>
    public required string GroupId { get; init; }

    /// <summary>The identifier of this emoji's symbol within its group's sprite.</summary>
    public required string SpriteId { get; init; }

    /// <summary>Which of the group's sprite sheets carries this emoji's symbol.</summary>
    public required int ChunkIndex { get; init; }
}
