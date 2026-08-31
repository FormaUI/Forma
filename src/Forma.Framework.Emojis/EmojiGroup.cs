namespace Forma.Framework.Emojis;

/// <summary>One category an emoji is filed under.</summary>
public sealed record EmojiGroup
{
    /// <summary>The identifier a catalog's entries name this group by.</summary>
    public required string Id { get; init; }

    /// <summary>The English source string naming this group, used as a localization key.</summary>
    public required string Title { get; init; }
}
