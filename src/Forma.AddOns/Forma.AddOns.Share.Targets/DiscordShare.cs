namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization.Metadata;

using Forma.Framework.Icons;

/// <summary>Posts a share item into the Discord channel a webhook address names.</summary>
/// <param name="webhookAddress">The Discord channel webhook's address.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public sealed class DiscordShare(Uri webhookAddress, HttpClient? http = null) : WebhookShare("discord", "Discord", Brand.Discord, webhookAddress, http)
{
    /// <inheritdoc />
    protected override JsonTypeInfo PayloadType
        => ShareTargetsJsonContext.Default.DiscordMessage;

    /// <inheritdoc />
    protected override object? GetPayload(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if ((item.Address ?? GetReachableSource(item)) is not { } address)
        {
            return null;
        }

        return new DiscordMessage(string.IsNullOrEmpty(item.Name) ? address : $"{item.Name}\n{address}");
    }
}
