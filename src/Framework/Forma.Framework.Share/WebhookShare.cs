namespace Forma.Framework.Share;

using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

using Forma.Framework.Icons;

/// <summary>The shell a webhook-reached target stands on: it posts the payload it makes of an item to the address the host configured.</summary>
/// <param name="id">The unique identifier of the target.</param>
/// <param name="title">The target's displayed title.</param>
/// <param name="icon">The icon drawn with the title.</param>
/// <param name="webhookAddress">The address the payload is posted to.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public abstract class WebhookShare(string id, string title, Icon icon, Uri webhookAddress, HttpClient? http) : ApiShare(id, title, icon, http)
{
    private readonly Uri _webhookAddress = webhookAddress ?? throw new ArgumentNullException(nameof(webhookAddress));

    /// <inheritdoc />
    public override bool Supports(ShareItem item)
        => GetPayload(item) is not null;

    /// <inheritdoc />
    public override async Task<bool> ShareAsync(ShareItem item, CancellationToken cancellationToken = default)
    {
        if (GetPayload(item) is not { } payload)
        {
            return false;
        }

        using var content = JsonContent.Create(payload, PayloadType);
        using var response = await Http.PostAsync(_webhookAddress, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return true;
    }

    /// <summary>The JSON type information that writes the payload <see cref="GetPayload"/> makes.</summary>
    protected abstract JsonTypeInfo PayloadType { get; }

    /// <summary>The payload posted for the item, where null means the target has nothing to post for it.</summary>
    protected abstract object? GetPayload(ShareItem item);
}
