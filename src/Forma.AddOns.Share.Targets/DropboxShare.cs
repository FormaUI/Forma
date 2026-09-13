namespace Forma.Framework.Share.Targets;

using System.Net.Http.Headers;
using System.Net.Http.Json;

using Forma.Framework.Icons;

/// <summary>Saves a share item's resource into the Dropbox of the account the user connected.</summary>
/// <param name="tokens">Answers the user's Dropbox token.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public sealed class DropboxShare(IShareTokenSource tokens, HttpClient? http = null) : AccountShare("dropbox", "Dropbox", Brand.Dropbox, tokens, http)
{
    private static readonly Uri SaveUrlEndpoint = new("https://api.dropboxapi.com/2/files/save_url");

    /// <summary>Dropbox fetches the file itself, so the item needs a resource living at a public address.</summary>
    public override bool Supports(ShareItem item)
        => GetReachableSource(item) is not null;

    /// <inheritdoc />
    protected override async Task ShareAsync(ShareItem item, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        using var request = new HttpRequestMessage(HttpMethod.Post, SaveUrlEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { path = $"/{item.Resource!.Name}", url = GetReachableSource(item) });

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
