namespace Forma.UI.Features.Share;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Forma.UI.Design.Icons;

/// <summary>Uploads a share item's resource into the Google Drive of the account whose token the host supplies.</summary>
public sealed class GoogleDriveShare : ApiShare
{
    private static readonly Uri UploadEndpoint = new("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart");

    private readonly Func<Task<string>> _accessToken;

    /// <summary>Creates a target uploading into the account the token accessor answers for.</summary>
    public GoogleDriveShare(Func<Task<string>> accessToken, HttpClient? http = null)
        : base("google-drive", "Google Drive", Brand.GoogleDrive, http)
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

        using var metadata = new StringContent(JsonSerializer.Serialize(new { name = item.Resource!.Name }), Encoding.UTF8, "application/json");
        using var media = new ByteArrayContent(bytes);
        using var content = new MultipartContent("related") { metadata, media };

        using var request = new HttpRequestMessage(HttpMethod.Post, UploadEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _accessToken().ConfigureAwait(false));
        request.Content = content;

        using var response = await Http.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
