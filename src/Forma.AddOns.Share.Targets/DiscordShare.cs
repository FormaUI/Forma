namespace Forma.Framework.Share.Targets;

using Forma.Framework.Icons;

/// <summary>Posts a share item into the Discord channel a webhook address names.</summary>
/// <param name="webhookAddress">The channel webhook's address, from Discord's channel settings.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public sealed class DiscordShare(Uri webhookAddress, HttpClient? http = null) : WebhookShare("discord", "Discord", Brand.Discord, webhookAddress, http)
{
    /// <summary>The channel message for the item: its name and the address it or its resource lives at; nothing when it lives nowhere reachable.</summary>
    protected override object? GetPayload(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if ((item.Address ?? GetReachableSource(item)) is not { } address)
        {
            return null;
        }

        return new { content = string.IsNullOrEmpty(item.Name) ? address : $"{item.Name}\n{address}" };
    }
}
