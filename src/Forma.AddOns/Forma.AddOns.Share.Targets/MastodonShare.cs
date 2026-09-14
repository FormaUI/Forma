namespace Forma.Framework.Share.Targets;

using Forma.Framework.Icons;

/// <summary>Opens the composer of the Mastodon server the user named, carrying the item's name and address as one post.</summary>
/// <param name="servers">Answers the server the user's Mastodon account lives on.</param>
public sealed class MastodonShare(IShareServerSource servers) : LinkShare("mastodon", "Mastodon", Brand.Mastodon)
{
    private readonly IShareServerSource _servers = servers ?? throw new ArgumentNullException(nameof(servers));

    /// <inheritdoc />
    public override bool RequiresAccount => true;

    /// <summary>A link or a picture is postable; whether the user has named a server decides where, not whether.</summary>
    public override bool Supports(ShareItem item)
        => EscapeText(item) is not null;

    /// <summary>The composer on the user's server, or null until the user names one.</summary>
    public override string? GetAddress(ShareItem item)
        => EscapeText(item) is { } text && _servers.GetServer(Id) is { } server
            ? $"{server.GetLeftPart(UriPartial.Authority)}/share?text={text}"
            : null;
}
