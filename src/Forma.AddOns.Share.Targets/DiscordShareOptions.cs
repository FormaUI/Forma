namespace Forma.Framework.Share.Targets;

/// <summary>What the host configures for sharing into a Discord channel.</summary>
public sealed class DiscordShareOptions
{
    /// <summary>The channel webhook's address, from Discord's channel settings.</summary>
    public Uri? WebhookAddress { get; set; }
}
