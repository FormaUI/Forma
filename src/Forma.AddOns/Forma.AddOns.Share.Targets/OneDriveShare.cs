namespace Forma.Framework.Share.Targets;

using System.Net.Http.Headers;

using Forma.Framework.Icons;

/// <summary>Uploads a share item's resource into the OneDrive of the account the user connected.</summary>
/// <param name="tokens">The source of the user's Microsoft Graph token.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public sealed class OneDriveShare(IShareTokenSource tokens, HttpClient? http = null) : AccountShare("onedrive", "OneDrive", Glyph.CloudUpload, tokens, http)
{
    /// <inheritdoc />
    public override bool Supports(ShareItem item)
        => GetReachableSource(item) is not null;

    /// <inheritdoc />
    protected override async Task ShareAsync(ShareItem item, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        var bytes = await Http.GetByteArrayAsync(new Uri(GetReachableSource(item)!), cancellationToken).ConfigureAwait(false);

        var address = new Uri($"https://graph.microsoft.com/v1.0/me/drive/root:/{Uri.EscapeDataString(item.Resource!.Name)}:/content");
        using var request = new HttpRequestMessage(HttpMethod.Put, address);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new ByteArrayContent(bytes);

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
