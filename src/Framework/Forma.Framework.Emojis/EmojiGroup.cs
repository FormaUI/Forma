namespace Forma.Framework.Emojis;

/// <summary>One category an emoji is filed under.</summary>
public sealed record EmojiGroup
{
    /// <summary>The identifier of this group.</summary>
    public required string Id { get; init; }

    /// <summary>The English source string naming this group, which is also its localization key.</summary>
    public required string Title { get; init; }
}
