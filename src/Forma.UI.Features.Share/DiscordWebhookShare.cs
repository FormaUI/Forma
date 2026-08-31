namespace Forma.UI.Features.Share;

using System.Net.Http.Json;

using Forma.UI.Design.Icons;

/// <summary>Posts a share item into the Discord channel a webhook address names.</summary>
public sealed class DiscordWebhookShare : ApiShare
{
    private readonly Uri _webhook;

    /// <summary>Creates a target posting to the webhook, over its own connection or one the caller supplies.</summary>
    public DiscordWebhookShare(Uri webhook, HttpClient? http = null)
        : base("discord", "Discord", Brand.Discord, http)
    {
        ArgumentNullException.ThrowIfNull(webhook);

        _webhook = webhook;
    }

    /// <summary>An item is postable when it carries an address, or a resource living at one.</summary>
    public override bool Supports(ShareItem item)
        => MessageOf(item) is not null;

    /// <inheritdoc />
    public override async Task ShareAsync(ShareItem item)
    {
        if (MessageOf(item) is not { } content)
        {
            return;
        }

        using var response = await Http.PostAsJsonAsync(_webhook, new { content }).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>The channel message for the item: its name and the address it or its resource lives at, or null when it lives nowhere reachable.</summary>
    private static string? MessageOf(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var address = item.Address ?? ReachableSource(item);

        if (address is null)
        {
            return null;
        }

        return string.IsNullOrEmpty(item.Name) ? address : $"{item.Name}\n{address}";
    }
}
