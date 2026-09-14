namespace Forma.Framework.Share.Targets;

using System.Net.Http.Headers;
using System.Net.Http.Json;

using Forma.Framework.Icons;

/// <summary>Starts publishing a share item's video on the TikTok account the user connected; TikTok pulls the video from the resource's address.</summary>
/// <param name="tokens">Answers the user's TikTok token.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public sealed class TikTokShare(IShareTokenSource tokens, HttpClient? http = null) : AccountShare("tiktok", "TikTok", Brand.TikTok, tokens, http)
{
    private static readonly Uri PublishEndpoint = new("https://open.tiktokapis.com/v2/post/publish/video/init/");

    /// <summary>TikTok pulls the video itself, so the item needs a resource living at a public address.</summary>
    public override bool Supports(ShareItem item)
        => GetReachableSource(item) is not null;

    /// <inheritdoc />
    protected override async Task ShareAsync(ShareItem item, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        using var request = new HttpRequestMessage(HttpMethod.Post, PublishEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            post_info = new { title = item.Name ?? item.Resource!.Name, privacy_level = "SELF_ONLY" },
            source_info = new { source = "PULL_FROM_URL", video_url = GetReachableSource(item) },
        });

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
