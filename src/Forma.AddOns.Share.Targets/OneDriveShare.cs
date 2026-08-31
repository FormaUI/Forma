namespace Forma.Framework.Share.Targets;

using System.Net.Http.Headers;

using Forma.Framework.Icons;

/// <summary>Uploads a share item's resource into the OneDrive of the account whose token the host supplies, through Microsoft Graph.</summary>
public sealed class OneDriveShare : ApiShare
{
    private readonly Func<Task<string>> _accessToken;

    /// <summary>Creates a target uploading into the account the token accessor answers for.</summary>
    public OneDriveShare(Func<Task<string>> accessToken, HttpClient? http = null)
        : base("onedrive", "OneDrive", Glyph.CloudUpload, http)
    {
        ArgumentNullException.ThrowIfNull(accessToken);

        _accessToken = accessToken;
    }

    /// <summary>The upload carries the bytes, so the item needs a resource living at a public address to fetch them from.</summary>
    public override bool Supports(ShareItem item)
        => ReachableSource(item) is not null;

    /// <inheritdoc />
    public override async Task ShareAsync(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (ReachableSource(item) is not { } source)
        {
            return;
        }

        var bytes = await Http.GetByteArrayAsync(new Uri(source)).ConfigureAwait(false);

        var address = new Uri($"https://graph.microsoft.com/v1.0/me/drive/root:/{Uri.EscapeDataString(item.Resource!.Name)}:/content");
        using var request = new HttpRequestMessage(HttpMethod.Put, address);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _accessToken().ConfigureAwait(false));
        request.Content = new ByteArrayContent(bytes);

        using var response = await Http.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
