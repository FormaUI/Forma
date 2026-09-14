namespace Forma.Framework.Share.Targets;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Forma.Framework.Icons;

/// <summary>Uploads a share item's resource into the Google Drive of the account the user connected.</summary>
/// <param name="tokens">Answers the user's Google Drive token.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public sealed class GoogleDriveShare(IShareTokenSource tokens, HttpClient? http = null) : AccountShare("google-drive", "Google Drive", Brand.GoogleDrive, tokens, http)
{
    private static readonly Uri UploadEndpoint = new("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart");

    /// <summary>The upload carries the bytes, so the item needs a resource living at a public address to fetch them from.</summary>
    public override bool Supports(ShareItem item)
        => GetReachableSource(item) is not null;

    /// <inheritdoc />
    protected override async Task ShareAsync(ShareItem item, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        var bytes = await Http.GetByteArrayAsync(new Uri(GetReachableSource(item)!), cancellationToken).ConfigureAwait(false);

        using var metadata = new StringContent(JsonSerializer.Serialize(new { name = item.Resource!.Name }), Encoding.UTF8, "application/json");
        using var media = new ByteArrayContent(bytes);
        using var content = new MultipartContent("related") { metadata, media };

        using var request = new HttpRequestMessage(HttpMethod.Post, UploadEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = content;

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
