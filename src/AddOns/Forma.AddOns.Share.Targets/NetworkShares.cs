namespace Forma.Framework.Share.Targets;

using Forma.Framework.Icons;

/// <summary>Opens Facebook's sharer on the item's address.</summary>
public sealed class FacebookShare() : LinkShare("facebook", "Facebook", Brand.Facebook)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeAddress(item) is { } address ? $"https://www.facebook.com/sharer/sharer.php?u={address}" : null;
}

/// <summary>Opens X's post composer carrying the item's name and address.</summary>
public sealed class XShare() : LinkShare("x", "X", Brand.X)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeAddress(item) is { } address ? $"https://x.com/intent/post?url={address}&text={EscapeName(item)}" : null;
}

/// <summary>Opens WhatsApp's composer carrying the item's name and address as one message.</summary>
public sealed class WhatsAppShare() : LinkShare("whatsapp", "WhatsApp", Brand.WhatsApp)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeText(item) is { } text ? $"https://wa.me/?text={text}" : null;
}

/// <summary>Opens Telegram's share screen on the item's address and name.</summary>
public sealed class TelegramShare() : LinkShare("telegram", "Telegram", Brand.Telegram)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeAddress(item) is { } address ? $"https://t.me/share/url?url={address}&text={EscapeName(item)}" : null;
}

/// <summary>Opens Bluesky's composer carrying the item's name and address as one post.</summary>
public sealed class BlueskyShare() : LinkShare("bluesky", "Bluesky", Brand.Bluesky)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeText(item) is { } text ? $"https://bsky.app/intent/compose?text={text}" : null;
}

/// <summary>Opens Reddit's submit screen on the item's address and name.</summary>
public sealed class RedditShare() : LinkShare("reddit", "Reddit", Brand.Reddit)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeAddress(item) is { } address ? $"https://www.reddit.com/submit?url={address}&title={EscapeName(item)}" : null;
}

/// <summary>Opens Snapchat's web share on the item's address.</summary>
public sealed class SnapchatShare() : LinkShare("snapchat", "Snapchat", Brand.Snapchat)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeAddress(item) is { } address ? $"https://www.snapchat.com/share?link={address}" : null;
}

/// <summary>Opens LinkedIn's share composer on the item's address.</summary>
public sealed class LinkedInShare() : LinkShare("linkedin", "LinkedIn", Brand.LinkedIn)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeAddress(item) is { } address ? $"https://www.linkedin.com/sharing/share-offsite/?url={address}" : null;
}

/// <summary>Opens Pinterest's pin composer on a picture, pointed at its media.</summary>
public sealed class PinterestShare() : LinkShare("pinterest", "Pinterest", Brand.Pinterest)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is not { Kind: ShareItemKind.Image } || EscapeAddress(item) is not { } address)
        {
            return null;
        }

        var media = Uri.EscapeDataString(item.Resource?.Source ?? item.Address!);

        return $"https://pinterest.com/pin/create/button/?url={address}&media={media}&description={EscapeName(item)}";
    }
}
