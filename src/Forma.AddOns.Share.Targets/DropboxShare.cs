namespace Forma.Framework.Share.Targets;

using System.Net.Http.Headers;
using System.Net.Http.Json;

using Forma.Framework.Icons;

/// <summary>Saves a share item's resource into the Dropbox of the account whose token the host supplies.</summary>
public sealed class DropboxShare : ApiShare
{
    private static readonly Uri SaveUrlEndpoint = new("https://api.dropboxapi.com/2/files/save_url");

    private readonly Func<Task<string>> _accessToken;

    /// <summary>Creates a target saving through the account the token accessor answers for.</summary>
    public DropboxShare(Func<Task<string>> accessToken, HttpClient? http = null)
        : base("dropbox", "Dropbox", Brand.Dropbox, http)
    {
        ArgumentNullException.ThrowIfNull(accessToken);

        _accessToken = accessToken;
    }

    /// <summary>Dropbox fetches the file itself, so the item needs a resource living at a public address.</summary>
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

        using var request = new HttpRequestMessage(HttpMethod.Post, SaveUrlEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _accessToken().ConfigureAwait(false));
        request.Content = JsonContent.Create(new { path = $"/{item.Resource!.Name}", url = source });

        using var response = await Http.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
