namespace Forma.Framework.Share.Targets;

using Forma.Framework.Icons;

/// <summary>Opens Facebook's sharer on the item's address.</summary>
public sealed class FacebookShare() : LinkShare("facebook", "Facebook", Brand.Facebook)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapedAddress(item) is { } address ? $"https://www.facebook.com/sharer/sharer.php?u={address}" : null;
}

/// <summary>Opens X's post composer carrying the item's name and address.</summary>
public sealed class XShare() : LinkShare("x", "X", Brand.X)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapedAddress(item) is { } address ? $"https://x.com/intent/post?url={address}&text={EscapedName(item)}" : null;
}

/// <summary>Opens WhatsApp's composer carrying the item's name and address as one message.</summary>
public sealed class WhatsAppShare() : LinkShare("whatsapp", "WhatsApp", Brand.WhatsApp)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapedText(item) is { } text ? $"https://wa.me/?text={text}" : null;
}

/// <summary>Opens Telegram's share screen on the item's address and name.</summary>
public sealed class TelegramShare() : LinkShare("telegram", "Telegram", Brand.Telegram)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapedAddress(item) is { } address ? $"https://t.me/share/url?url={address}&text={EscapedName(item)}" : null;
}

/// <summary>Opens Bluesky's composer carrying the item's name and address as one post.</summary>
public sealed class BlueskyShare() : LinkShare("bluesky", "Bluesky", Brand.Bluesky)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapedText(item) is { } text ? $"https://bsky.app/intent/compose?text={text}" : null;
}

/// <summary>Opens Reddit's submit screen on the item's address and name.</summary>
public sealed class RedditShare() : LinkShare("reddit", "Reddit", Brand.Reddit)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapedAddress(item) is { } address ? $"https://www.reddit.com/submit?url={address}&title={EscapedName(item)}" : null;
}

/// <summary>Opens Snapchat's web share on the item's address.</summary>
public sealed class SnapchatShare() : LinkShare("snapchat", "Snapchat", Brand.Snapchat)
{
    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapedAddress(item) is { } address ? $"https://www.snapchat.com/share?link={address}" : null;
}

/// <summary>Opens Pinterest's pin composer on a picture, pointed at its media.</summary>
public sealed class PinterestShare() : LinkShare("pinterest", "Pinterest", Brand.Pinterest)
{
    /// <summary>A board pins media, so only a picture opens here.</summary>
    public override string? GetAddress(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is not { Kind: ShareItemKind.Image } || EscapedAddress(item) is not { } address)
        {
            return null;
        }

        var media = Uri.EscapeDataString(item.Resource?.Source ?? item.Address!);

        return $"https://pinterest.com/pin/create/button/?url={address}&media={media}&description={EscapedName(item)}";
    }
}
