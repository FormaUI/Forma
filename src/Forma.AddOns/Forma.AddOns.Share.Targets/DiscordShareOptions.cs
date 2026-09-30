namespace Forma.Framework.Share.Targets;

/// <summary>The options for sharing into a Discord channel.</summary>
public sealed class DiscordShareOptions
{
    /// <summary>The Discord channel webhook's address.</summary>
    public Uri? WebhookAddress { get; set; }
}
